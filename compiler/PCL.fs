﻿namespace Compiler

open FSharp.Text.Lexing
open CodeGenerator
open LLVMSharp

module PCL =

  let private verifyAndDump _module =
    if LLVM.VerifyModule (_module, LLVMVerifierFailureAction.LLVMPrintMessageAction, ref null) <> LLVMBool 0 then
      printfn "Erroneuous module"
    else
      LLVM.DumpModule _module
      // LLVM.PrintModuleToFile (_module, "test.txt", ref null) |> ignore

  let private combined program = async {
    let! semantic = Async.StartChild <| async { return Engine.Analyze program }
    let! arBuilder = Async.StartChild <| async { return GenerateARTypes program }

    let! res1 = semantic
    let! res2 = arBuilder

    return (res1, res2)
  }

  [<EntryPoint>]
  let main argv =
    (* Get the filename that is to be processed and store it for future reference *)
    let filename = if argv.Length >= 1 then argv.[0] else "../examples/semInstructions.pcl"
    Helpers.Error.FileName <- System.IO.Path.GetFullPath filename

    (* Setup the input text *)
    let input = System.IO.File.ReadAllText filename

    (* Parse and perform semantic analysis *)
    try
      let parse input =
        let lexbuf = LexBuffer<_>.FromString input
        Parser.start Lexer.read lexbuf

      match parse input with
      | Some program -> printfn "errors:\n%A" Helpers.Error.Parser.errorList

                        let semanticAnalysis, semanticInstruction = Engine.Analyze program
                        let arTypes = GenerateARTypes program

                        if not(semanticAnalysis) then
                          printfn "Semantic Analysis failed. Goodbye..."
                          exit 1

                        let topLevelFunction = 
                          match semanticInstruction with
                          | Base.SemDeclFunction (n, t, il) -> (n, t, il)
                          | _                      -> raise <| Helpers.Error.InternalException "Top Level Instruction must be a function"

                        printfn "%A" arTypes
                        printfn "%A" semanticInstruction
                        
                        let flatTuple (a, (b,c)) = (a, b, c)
                        let normalizedHierarchy = Engine.NormalizeInstructionHierarchy topLevelFunction |> Map.toList
                        let normalizedHierarchy = List.map flatTuple normalizedHierarchy
                        // printfn "%A" <| (Engine.NormalizeInstructionHierarchy topLevelFunction)
                        // printfn "%A" <| ((Engine.NormalizeInstructionHierarchy topLevelFunction) |> Map.toList |> List.map fst)

                        // let theModule, _ = Engine.Generate semanticInstructions
                        // Can run in parallel with a few adjustments in AR type generation
                        // let semanticAnalysis, arTypes =
                        //   combined program |> 
                        //   Async.RunSynchronously

                        let theModule, theBuilder = Engine.Generate arTypes normalizedHierarchy
                        verifyAndDump theModule
                       
      | None -> printfn "errors:\n%A\n\nNo input given" Helpers.Error.Parser.errorList
    with
      | Helpers.Error.Lexer.LexerException e -> printfn "Lex Exception -> %s" <| Helpers.Error.StringifyError e
      | Helpers.Error.Parser.ParserException e -> printfn "Parse Exception -> %s" <| Helpers.Error.StringifyError e
      | Helpers.Error.Semantic.SemanticException e -> printfn "Semantic Exception -> %s" <| Helpers.Error.StringifyError e
      | Helpers.Error.Symbolic.SymbolicException e -> printfn "Symbolic Exception -> %s" <| Helpers.Error.StringifyError e
      | e -> printfn "%A" e

    (* LLVM *)
    // let theModule, theBuilder = GenerateMain ()

    // // let ctx = LLVM.ContextCreate ()
    
    // GenerateGlobalVariable "theInteger" <| Base.Integer
    // GenerateGlobalVariable "theBoolean" <| Base.Boolean
    // GenerateGlobalVariable "theCharacter" <| Base.Character
    // GenerateGlobalVariable "theReal" <| Base.Real
    // GenerateGlobalVariable "theArray" <| Base.Array (Base.Integer, 2)
    // GenerateGlobalVariable "the2DArray" <| Base.Array (Base.Array (Base.Integer, 2), 4)
    // GenerateGlobalVariable "thePointer" <| Base.Ptr Base.Integer

    // // let addStruct = GenerateStructType ((Base.Ptr Base.Character).ToLLVM ()) [Base.Integer; Base.Integer; Base.Integer]
    // // let multStruct = GenerateStructType (LLVM.PointerType (addStruct, 0u)) [Base.Integer; Base.Integer]

    // // GenerateFunction "addFunction" addStruct Base.Integer [] |> ignore
    // // GenerateFunction "addFunction.Multiply" multStruct Base.Integer [] |> ignore
    // GenerateFunctionRogue "writeInteger" [Base.Integer] Base.Unit [LLVMLinkage.LLVMExternalLinkage] |> ignore
    // GenerateFunctionRogue "writeReal" [Base.Real] Base.Unit [LLVMLinkage.LLVMExternalLinkage] |> ignore

    // // // Multiply Function

    // // let multiplyFunction = LLVM.GetNamedFunction (theModule, "addFunction.Multiply")
    // // GenerateBasicBlock multiplyFunction "entry" |> ignore

    // // let p = multiplyFunction.GetFirstParam ()

    // // let myD = GenerateStructLoad p 1

    // // let alink = GenerateStructLoad p 0
    // // let parentA = GenerateStructLoad alink 1
    // // let parentC = GenerateStructLoad alink 3

    // // let mul = generateBinop Base.Mult parentA myD false
    // // let add = generateBinop Base.Add mul parentC false

    // // LLVM.BuildRet (theBuilder, add) |> ignore

    // // // Add Function

    // // let addFunction = LLVM.GetNamedFunction (theModule, "addFunction")
    // // GenerateBasicBlock addFunction "entry" |> ignore

    // // let p = addFunction.GetFirstParam ()

    // // // c = 10
    // // GenerateStructStore p 3 (LLVM.ConstInt(LLVM.Int32Type (), 10UL, LLVMBool 1)) |> ignore

    // // // Fill activation record to call Multiply 5
    // // let ar = GenerateLocal multStruct

    // // GenerateStructStore ar 0 p |> ignore
    // // GenerateStructStore ar 1 (LLVM.ConstInt(LLVM.Int32Type (), 5UL, LLVMBool 1)) |> ignore

    // // let call = GenerateFunctionCall "addFunction.Multiply" [|ar|]
    // // let loadB = GenerateStructLoad p 2
    // // let add = LLVM.BuildAdd (theBuilder, loadB, call, "tempadd")

    // // LLVM.BuildRet (theBuilder, add) |> ignore

    // // // Main Function

    // // let theMain = LLVM.GetNamedFunction (theModule, "main")
    // // let theBasicBlock = LLVM.GetEntryBasicBlock theMain
    // // let theRet = LLVM.GetFirstInstruction theBasicBlock
    // // LLVM.PositionBuilderBefore (theBuilder, theRet)

    // // // Fill activation record to call AddFunction 3 5
    // // let ar = GenerateLocal addStruct
    // // GenerateStructStore ar 0 (LLVM.ConstNull((Base.Ptr Base.Character).ToLLVM ())) |> ignore
    // // GenerateStructStore ar 1 (LLVM.ConstInt(LLVM.Int32Type (), 3UL, LLVMBool 1)) |> ignore
    // // GenerateStructStore ar 2 (LLVM.ConstInt(LLVM.Int32Type (), 5UL, LLVMBool 1)) |> ignore

    // // let res = GenerateFunctionCall "addFunction" [|ar|]
    // // GenerateFunctionCall "writeInteger" [|res|] |> ignore

    // // // let theNull() = LLVMValueRef (nativeint 0)
    // // // let nil = LLVM.ConstNull (LLVM.GetElementType (gep.TypeOf ()))      // This is how nil gets the correct type

    // let theMain = LLVM.GetNamedFunction (theModule, "main")
    // let theBasicBlock = LLVM.GetEntryBasicBlock theMain
    // let theRet = LLVM.GetFirstInstruction theBasicBlock
    // LLVM.PositionBuilderBefore (theBuilder, theRet)
    
    // let theGlobal = LLVM.GetNamedGlobal (theModule, "theInteger")
    // LLVM.BuildStore (theBuilder, LLVM.ConstInt (LLVM.Int32Type(), 0UL, LLVMBool 0), theGlobal) |> ignore

    // // let symTable = SymbolTable.CreateSymbolTable ()
    // // let _, symTable = SymbolTable.OpenScope symTable "newScope" Base.Unit
    // // let symTable = SymbolTable.AddDeclarationToTable symTable (SymbolTable.Variable ("x", Base.Integer))
    // // let newScopeAR = GenerateStructType ((Base.Ptr Base.Character).ToLLVM ()) [Base.Integer] 

    // // let curAR = GenerateLocal newScopeAR
    // // GenerateStructStore curAR 1 (LLVM.ConstInt (LLVM.Int32Type(), 10UL, LLVMBool 1)) |> ignore

    // // let _, symTable = SymbolTable.OpenScope symTable "newScope2" Base.Unit
    // // let newScope2AR = GenerateStructType (LLVM.PointerType(newScopeAR, 0u)) []

    // // let curAR2 = GenerateLocal newScope2AR
    // // GenerateStructStore curAR2 0 curAR |> ignore    

    // // let _, symTable = SymbolTable.OpenScope symTable "newScope3" Base.Unit
    // // let symTable = SymbolTable.AddDeclarationToTable symTable (SymbolTable.Variable ("y", Base.Integer))
    // // let symTable = SymbolTable.AddDeclarationToTable symTable (SymbolTable.Variable ("z", Base.Integer))
    // // let symTable = SymbolTable.AddDeclarationToTable symTable (SymbolTable.Variable ("b", Base.Boolean))
    // // let newScope3AR = GenerateStructType (LLVM.PointerType(newScope2AR, 0u)) [Base.Integer; Base.Integer; Base.Boolean] 

    // // let curAR3 = GenerateLocal newScope3AR
    // // GenerateStructStore curAR3 0 curAR2 |> ignore
    // // GenerateStructStore curAR3 1 (LLVM.ConstInt (LLVM.Int32Type(), 20UL, LLVMBool 1)) |> ignore
    // // GenerateStructStore curAR3 2 (LLVM.ConstInt (LLVM.Int32Type(), 35UL, LLVMBool 1)) |> ignore

    // // let expr = Base.RExpression <| Base.Binop (Base.LExpression <| Base.Identifier "y", Base.Add, Base.LExpression <| Base.Identifier "z")
    // // let expr = Base.RExpression <| Base.Binop (expr, Base.Mult, Base.LExpression <| Base.Identifier "x")
    // // let expr = Base.RExpression <| Base.Unop (Base.Negative, expr)

    // // let stmt = Base.Assign (Base.Identifier "x", expr, Unchecked.defaultof<Position>)

    // // let inst = Semantic.GenerateSemanticStatement symTable stmt
    // // let res = GenerateInstruction curAR3 inst

    // // GenerateFunctionCall "writeInteger" [|res|] |> ignore

    // // let integer = GenerateLocal (LLVM.Int32Type())
    // // let integer = LLVM.BuildLoad (theBuilder, integer, "tempload")

    // // let real = GenerateLocal (LLVM.X86FP80Type ())
    // // let real = LLVM.BuildLoad (theBuilder, real, "tempload")

    // // let realIntegered = LLVM.BuildFPToSI (theBuilder, real, integer.TypeOf (), "tempcast")
    // // let integerRealed = LLVM.BuildSIToFP (theBuilder, integer, real.TypeOf (), "tempcast")

    // // let theAdd = LLVM.BuildAdd (theBuilder, integer, realIntegered, "tempadd")

    // // let theFad = LLVM.BuildFAdd (theBuilder, integerRealed, real, "tempadd")

    // // GenerateFunctionCall "writeInteger" [|res|] |> ignore
    // // GenerateFunctionCall "writeReal" [|theFad|] |> ignore

    // if LLVM.VerifyModule (theModule, LLVMVerifierFailureAction.LLVMPrintMessageAction, ref null) <> LLVMBool 0 then
    //   printfn "Erroneuous module"
    // LLVM.DumpModule theModule

    // LLVM.PrintModuleToFile (theModule, "test.txt", ref null) |> ignore

    0
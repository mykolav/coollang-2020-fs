namespace rec LibCool.Frontend


open System
open System.Collections.Generic
open System.Text
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.Frontend.SemanticParts
open AstExtensions


[<Sealed>]
type private Scope() =    
    
    
    let _visible_vars = Dictionary<ID, VarSymbol>()
    
    
    member this.AddVisible(var_node: AstNode<VarSyntax>): unit =
        _visible_vars.Add(var_node.Syntax.ID.Syntax,
                          { VarSymbol.Name = var_node.Syntax.ID.Syntax
                            Type = var_node.Syntax.TYPE.Syntax
                            Index = _visible_vars.Count
                            SyntaxSpan = var_node.Span })
    
    
    member this.IsVisible(name: ID): bool = _visible_vars.ContainsKey(name)
    
    
    member this.Get(name: ID): VarSymbol = _visible_vars.[name]
    member this.TryGet(name: ID): VarSymbol voption =
        if _visible_vars.ContainsKey(name)
        then ValueSome (_visible_vars.[name])
        else ValueNone


[<Sealed>]
type private SymbolTableScopeGuard(_symbol_table: SymbolTable) =
    interface IDisposable with
        member this.Dispose() =
            _symbol_table.LeaveScope()


[<Sealed>]
type private SymbolTable(_class_sym: ClassSymbol) as this =


    let _scopes = Stack<Scope>()
    
    
    member private this.CurrentScope = _scopes.Peek()
    
    
    member this.CreateScope(): IDisposable =
        _scopes.Push(Scope())
        new SymbolTableScopeGuard(this) :> IDisposable
    
    
    member this.LeaveScope(): unit =
        _scopes.Pop() |> ignore
    
    
    member this.Resolve(name: ID): VarSymbol = raise (NotImplementedException())
    member this.TryResolve(name: ID): VarSymbol voption = ValueNone


[<Sealed>]
type private SemanticStage(_program_syntax: ProgramSyntax,
                           _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                           _diags: DiagnosticBag,
                           _source: Source) =
    
    
    let sb_data = StringBuilder()
    let sb_code = StringBuilder()
    
    
    let translate_attr (attr_node: AstNode<AttrSyntax>) =
        ()
    
    
    let translate_ctor (class_node: AstNode<ClassSyntax>) =
        // .ctor's formals are varformals,
        // .ctor's body is
        //   - an invocation of the super's .ctor with actuals from the extends syntax
        //   - assign values passed as formals to attrs derived from varformals
        //   - assign initial exprs to attrs defined in the class
        //   - concatenated blocks (if any)
        //   - ExprSyntax.This is always appended to the .ctor's end
        //     (as a result, the last block's last expr's type doesn't have to match the class' type)

        class_node.Syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsAttr)
            |> Seq.iter (fun feature_node -> translate_attr (feature_node.Map(fun it -> it.AsAttrSyntax)))
    
    
    let translate_method (method_node: AstNode<MethodSyntax>) =
        ()
    
    
    let translate_class (class_node: AstNode<ClassSyntax>) =
        translate_ctor class_node
        class_node.Syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsMethod)
            |> Seq.iter (fun feature_node -> translate_method (feature_node.Map(fun it -> it.AsMethodSyntax)))


    member this.Translate(): string =
        _program_syntax.Classes |> Array.iter translate_class
        ""


[<Sealed>]
type SemanticStageDriver private () =
        
    
    static member Translate(program_syntax: ProgramSyntax, diags: DiagnosticBag, source: Source): string =
        let classdecl_node_map = ClassDeclCollector(program_syntax, diags, source).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
            
        let class_sym_map = ClassSymbolCollector(program_syntax,
                                                 classdecl_node_map,
                                                 source,
                                                 diags).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
        
        let asm = SemanticStage(program_syntax, class_sym_map, diags, source).Translate()
        asm

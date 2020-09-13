namespace LibCool.Frontend.SemanticParts


open System
open System.Collections.Generic
open System.Text
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.SourceParts


module private AstExtensions =
    
    
    open Ast
    
    
    type Feature
        with
        member this.IsMethod: bool =
            match this with
            | Feature.Method _ -> true
            | _            -> false
        member this.AsMethodInfo: MethodInfo =
            match this with
            | Feature.Method it -> it 
            | _         -> invalidOp "Feature.MethodInfo"
        member this.IsAttr: bool =
            match this with
            | Feature.Attr _ -> true
            | _      -> false
        member this.AsAttrInfo: AttrInfo =
            match this with
            | Feature.Attr it -> it
            | _       -> invalidOp "Feature.AttrInfo"
        member this.IsBracedBlock: bool =
            match this with
            | Feature.BracedBlock _ -> true
            | _             -> false
        member this.AsBlockInfo: Block voption =
            match this with
            | Feature.BracedBlock it -> it 
            | _              -> invalidOp "Feature.BracedInfo"
            
            
    type ClassDecl
        with
        member this.Super: TYPENAME =
            match this.Extends with
            | ValueNone -> TYPENAME "Any"
            | ValueSome extends_node ->
                match extends_node.Value with
                | Extends.Info it -> it.SUPER.Value
                | Extends.Native  -> invalidOp "Extends.Native"


module Sema =


    type AttrSymbol =
        { Name: Ast.ID
          Type: Ast.TYPENAME
          DeclaringClass: Ast.TYPENAME
          Index: int
          Syntax: Ast.Node<Ast.AttrInfo> voption }
        with
        member this.SyntaxSpan =
            if this.Syntax.IsSome
            then this.Syntax.Value.Span
            else Span.Invalid


    type ParamSymbol =
        { Name: Ast.ID
          Type: Ast.TYPENAME
          Index: int
          SyntaxSpan: Span }


    type MethodSymbol =
        { Name: Ast.ID
          Params: ParamSymbol[]
          ReturnType: Ast.TYPENAME
          Override: bool
          DeclaringClass: Ast.TYPENAME
          Index: int
          Syntax: Ast.Node<Ast.MethodInfo> voption }
        with
        member this.SyntaxSpan =
            if this.Syntax.IsSome
            then this.Syntax.Value.Span
            else Span.Invalid


    type ClassSymbol =
        { Name: Ast.TYPENAME
          Super: Ast.TYPENAME
          Ctor: MethodSymbol
          Attrs: IReadOnlyDictionary<Ast.ID, AttrSymbol>
          Methods: IReadOnlyDictionary<Ast.ID, MethodSymbol>
          Syntax: Ast.Node<Ast.ClassDecl> voption }
        with
        member this.SyntaxSpan =
            if this.Syntax.IsSome
            then this.Syntax.Value.Span
            else Span.Invalid


[<RequireQualifiedAccess>]
module private BasicClassSymbols =
    
    
    open Sema
    
    
    let Any: ClassSymbol =
        { Name = Ast.TYPENAME "Any"
          Super = Ast.TYPENAME ""
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "Any"  
              Override = false
              DeclaringClass = Ast.TYPENAME "Any" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
    let Unit: ClassSymbol =
        { Name = Ast.TYPENAME "Unit"
          Super = Ast.TYPENAME "Any"
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "Unit"  
              Override = false
              DeclaringClass = Ast.TYPENAME "Unit" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
    let Int: ClassSymbol =
        { Name = Ast.TYPENAME "Int"
          Super = Ast.TYPENAME "Any"
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "Int"  
              Override = false
              DeclaringClass = Ast.TYPENAME "Int" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
    let String: ClassSymbol =
        { Name = Ast.TYPENAME "String"
          Super = Ast.TYPENAME "Any"
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "String"
              Override = false
              DeclaringClass = Ast.TYPENAME "String" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
    let Boolean: ClassSymbol =
        { Name = Ast.TYPENAME "Boolean"
          Super = Ast.TYPENAME "Any"
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "Boolean"  
              Override = false
              DeclaringClass = Ast.TYPENAME "Boolean" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
    let ArrayAny: ClassSymbol =
        { Name = Ast.TYPENAME "ArrayAny"
          Super = Ast.TYPENAME "Any"
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "ArrayAny"  
              Override = false
              DeclaringClass = Ast.TYPENAME "ArrayAny" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
    let IO: ClassSymbol =
        { Name = Ast.TYPENAME "IO"
          Super = Ast.TYPENAME "Any"
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "IO"  
              Override = false
              DeclaringClass = Ast.TYPENAME "IO" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
    let Symbol: ClassSymbol =
        { Name = Ast.TYPENAME "Symbol"
          Super = Ast.TYPENAME "Any"
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
              Params = [||] 
              ReturnType = Ast.TYPENAME "Symbol"  
              Override = false
              DeclaringClass = Ast.TYPENAME "Symbol" 
              Index = -1
              Syntax = ValueNone
            }
          Attrs = Map.empty
          Methods = Map.empty
          Syntax = ValueNone
        }

    
open AstExtensions
open Sema
    

[<Sealed>]
type private ClassDeclCollector(_program_syntax: Ast.Program, _diags: DiagnosticBag, _source: Source) =
    
    
    member this.Collect() =
        let map = Dictionary<Ast.TYPENAME, Ast.Node<Ast.ClassDecl>>()
        
        let add_classdecl_syntax (classdecl_node: Ast.Node<Ast.ClassDecl>): unit =
            let classdecl_syntax = classdecl_node.Value
            if map.ContainsKey(classdecl_syntax.NAME.Value)
            then
                let prev_classdecl_syntax = map.[classdecl_syntax.NAME.Value].Value
                let message = sprintf "The program already contains a class '%O' at %O"
                                      classdecl_syntax.NAME.Value
                                      (_source.Map(prev_classdecl_syntax.NAME.Span.First))
                                      
                _diags.Error(message, classdecl_syntax.NAME.Span)
            else
                map.Add(classdecl_syntax.NAME.Value, classdecl_node)
        
        _program_syntax.ClassDecls |> Seq.iter (fun it -> add_classdecl_syntax it)
        
        map :> IReadOnlyDictionary<_, _>


[<Sealed>]
type private ClassSymbolCollector(_program_syntax: Ast.Program,
                                  _classdecl_node_map: IReadOnlyDictionary<Ast.TYPENAME, Ast.Node<Ast.ClassDecl>>,
                                  _source: Source,
                                  _diags: DiagnosticBag) =

    
    let _classsym_map = Dictionary<Ast.TYPENAME, ClassSymbol>() 
    

    let mk_attr_sym (classdecl_syntax: Ast.ClassDecl)
                    (attr_node: Ast.Node<Ast.AttrInfo>)
                    (index: int): AttrSymbol =
        let attr_syntax = attr_node.Value
        { AttrSymbol.Name = attr_syntax.ID.Value
          Type = attr_syntax.TYPE.Value
          DeclaringClass = classdecl_syntax.NAME.Value
          Index = index
          Syntax = ValueSome attr_node }
    

    let mk_attr_syms (classdecl_syntax: Ast.ClassDecl)
                     (super: ClassSymbol) =
        let attr_syms = Dictionary<Ast.ID, AttrSymbol>(super.Attrs)

        let add_attr_sym i (feature_node: Ast.Node<Ast.Feature>) =
            let attr_node = feature_node.Map(fun it -> it.AsAttrInfo)
            let attr_syntax = attr_node.Value
            
            if attr_syms.ContainsKey(attr_syntax.ID.Value)
            then
                let prev_attr_sym = attr_syms.[attr_syntax.ID.Value]
                let sb_message =
                    StringBuilder().AppendFormat("The class '{0}' already contains an attribute '{1}' " +
                                                 "declared in the class '{2}'",
                                                 classdecl_syntax.NAME.Value,
                                                 attr_syntax.ID.Value,
                                                 prev_attr_sym.DeclaringClass)
                if prev_attr_sym.Syntax.IsSome
                then
                    sb_message.AppendFormat(" at {0}",
                                            _source.Map(prev_attr_sym.SyntaxSpan.First)) |> ignore
                    
                _diags.Error(sb_message.ToString(), feature_node.Span)
            else
                
            let ai = mk_attr_sym classdecl_syntax
                                 attr_node
                                 (super.Attrs.Count + i)
            attr_syms.Add(ai.Name, ai)
        
        classdecl_syntax.ClassBody
        |> Seq.where (fun it -> it.Value.IsAttr)
        |> Seq.iteri add_attr_sym
        
        attr_syms :> IReadOnlyDictionary<_, _>
    
    
    let mk_param_syms (method_syntax: Ast.MethodInfo) =
        let formal_node_map = Dictionary<Ast.ID, Ast.Node<Ast.Formal>>()

        method_syntax.Formals
        |> Array.iter (fun it ->
            let formal_syntax = it.Value
            if formal_node_map.ContainsKey(formal_syntax.ID.Value)
            then
                let prev_formal_node = formal_node_map.[formal_syntax.ID.Value]
                let message = String.Format("The method '{0}' already contains a formal '{1}' at {2}",
                                            method_syntax.ID.Value,
                                            formal_syntax.ID.Value,
                                            prev_formal_node.Span)
                _diags.Error(message, it.Span)
            else
                
            formal_node_map.Add(formal_syntax.ID.Value, it)
        )

        let param_syms = method_syntax.Formals
                          |> Array.mapi (fun i it -> { ParamSymbol.Name = it.Value.ID.Value
                                                       Type = it.Value.TYPE.Value
                                                       Index = i
                                                       SyntaxSpan = it.Span })
        param_syms


    let mk_method_sym (classdecl_syntax: Ast.ClassDecl)
                      (method_node: Ast.Node<Ast.MethodInfo>)
                      (index: int): MethodSymbol =
        let method_syntax = method_node.Value
        let param_syms = mk_param_syms method_syntax

        { MethodSymbol.Name = method_syntax.ID.Value
          Params = param_syms
          ReturnType = method_syntax.RETURN.Value
          Override = method_syntax.Override
          DeclaringClass = classdecl_syntax.NAME.Value
          Index = index
          Syntax = ValueSome method_node }
        
        
    let mk_method_syms (classdecl_syntax: Ast.ClassDecl)
                       (super: ClassSymbol) =
        let method_syms = Dictionary<Ast.ID, MethodSymbol>(super.Methods)
        
        let add_method_sym i (feature_node: Ast.Node<Ast.Feature>) =
            let method_node = feature_node.Map(fun it -> it.AsMethodInfo)
            let method_syntax = method_node.Value
            if method_syms.ContainsKey(method_syntax.ID.Value)
            then
                let prev_method_sym = method_syms.[method_syntax.ID.Value]
                let sb_message =
                    StringBuilder().AppendFormat("The class '{0}' already contains a method '{1}' " +
                                                 "declared in the class '{2}'",
                                                 classdecl_syntax.NAME.Value,
                                                 method_syntax.ID.Value,
                                                 prev_method_sym.DeclaringClass)
                if prev_method_sym.Syntax.IsSome
                then
                    sb_message.AppendFormat(" at {0}",
                                            _source.Map(prev_method_sym.SyntaxSpan.First)) |> ignore
                    
                _diags.Error(sb_message.ToString(), feature_node.Span)
            else
            
            let mi = mk_method_sym classdecl_syntax
                                   method_node
                                   (super.Methods.Count + i)
            method_syms.Add(mi.Name, mi)

        classdecl_syntax.ClassBody
        |> Seq.where (fun it -> it.Value.IsMethod)
        |> Seq.iteri (fun i it -> add_method_sym i it) 

        method_syms :> IReadOnlyDictionary<_, _>


    let mk_ctor_param_syms (classdecl_syntax: Ast.ClassDecl) =
        let varformal_node_map = Dictionary<Ast.ID, Ast.Node<Ast.VarFormal>>()

        classdecl_syntax.VarFormals
        |> Array.iter (fun it ->
            let varformal_syntax = it.Value
            if varformal_node_map.ContainsKey(varformal_syntax.ID.Value)
            then
                let prev_varformal_node = varformal_node_map.[varformal_syntax.ID.Value]
                let message = String.Format("The constructor of class '{0}' already contains a var formal '{1}' at {2}",
                                            classdecl_syntax.NAME.Value,
                                            varformal_syntax.ID.Value,
                                            prev_varformal_node.Span)
                _diags.Error(message, it.Span)
            else
                
            varformal_node_map.Add(varformal_syntax.ID.Value, it)
        )

        let param_syms = classdecl_syntax.VarFormals
                         |> Array.mapi (fun i it -> { ParamSymbol.Name = it.Value.ID.Value
                                                      Type = it.Value.TYPE.Value
                                                      Index = i
                                                      SyntaxSpan = it.Span })
        param_syms


    let mk_ctor_sym (classdecl_syntax: Ast.ClassDecl): MethodSymbol =
        let param_syms = mk_ctor_param_syms classdecl_syntax
        
        { MethodSymbol.Name = Ast.ID ".ctor"
          Params = param_syms
          ReturnType = classdecl_syntax.NAME.Value
          Override = false
          DeclaringClass = classdecl_syntax.NAME.Value
          Index = -1
          Syntax = ValueNone }
    

    let mk_class_sym (classdecl_node: Ast.Node<Ast.ClassDecl>)
                     (super: ClassSymbol) : ClassSymbol =
        let classdecl_syntax = classdecl_node.Value
        let attr_syms = mk_attr_syms classdecl_syntax super 
        let method_syms = mk_method_syms classdecl_syntax super
        
        { ClassSymbol.Name = classdecl_syntax.NAME.Value
          Super = super.Name
          Ctor = mk_ctor_sym classdecl_syntax
          Attrs = attr_syms
          Methods = method_syms
          Syntax = ValueSome classdecl_node }
        
        
    let add_class_sym (classsym: ClassSymbol): unit =
        _classsym_map.Add(classsym.Name, classsym)


    let rec add_classsym_of (classname: Ast.TYPENAME): ClassSymbol =
        if _classsym_map.ContainsKey(classname)
        then
            _classsym_map.[classname]
        else
            
        let classdecl_node = _classdecl_node_map.[classname]
        let supersym = add_classsym_of classdecl_node.Value.Super
        let classsym = mk_class_sym classdecl_node supersym
        
        add_class_sym classsym
        classsym
    
    
    member this.Collect(): IReadOnlyDictionary<Ast.TYPENAME, ClassSymbol> =
        add_class_sym BasicClassSymbols.Any
        add_class_sym BasicClassSymbols.Unit
        add_class_sym BasicClassSymbols.Int
        add_class_sym BasicClassSymbols.String
        add_class_sym BasicClassSymbols.Boolean
        add_class_sym BasicClassSymbols.ArrayAny
        add_class_sym BasicClassSymbols.IO
        add_class_sym BasicClassSymbols.Symbol
        
        _program_syntax.ClassDecls |> Array.iter (fun classdecl_node ->
            let classdecl = classdecl_node.Value
            add_classsym_of classdecl.NAME.Value |> ignore)
        
        _classsym_map :> IReadOnlyDictionary<_, _>


[<Sealed>]
type SemanticStage private () =
        
    
    static member Translate(program_syntax: Ast.Program, diags: DiagnosticBag, source: Source): string =
        let classdecl_node_map = ClassDeclCollector(program_syntax, diags, source).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
            
        let classsym_map = ClassSymbolCollector(
                               program_syntax,
                               classdecl_node_map,
                               source,
                               diags).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
        
        classsym_map |> ignore
        ""

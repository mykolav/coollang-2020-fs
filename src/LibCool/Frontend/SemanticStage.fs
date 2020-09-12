namespace LibCool.Frontend


open System.Collections.Generic
open LibCool.AstParts
open LibCool.SemanticParts.Sema
open LibCool.DiagnosticParts


[<RequireQualifiedAccess>]
module private ClassInfos =

    
    let private mk_method_info (ast_method_info: Ast.MethodInfo) (index: int) =
        let param_infos = ast_method_info.Formals
                          |> Array.mapi (fun i it -> { ParamInfo.Name = it.Value.ID.Value
                                                       Type = it.Value.TYPE_NAME.Value
                                                       Index = i })
        { MethodInfo.Name = ast_method_info.ID.Value
          Params = param_infos
          ReturnType = ast_method_info.TYPE_NAME.Value
          Override = ast_method_info.Override
          Index = index }
    

    let private collect_class_info (class_decl: Ast.ClassDecl): ClassInfo =
        let attr_infos = Map.empty
        
        let method_infos = List<Ast.ID * MethodInfo>()
        method_infos.AddRange(class_decl.ClassBody
                              |> Seq.where (fun it -> it.Value.IsMethod)
                              |> Seq.mapi (fun i it -> let mi = mk_method_info it.Value.MethodInfo i
                                                       mi.Name, mi))
        
        { ClassInfo.Name = class_decl.NAME.Value
          Attributes = attr_infos
          Methods = Map.ofSeq method_infos }
        
        
    let collect (program: Ast.Program): Map<Ast.TYPE_NAME, ClassInfo> =
        program.ClassDecls
        |> Seq.map (fun it -> let class_info = collect_class_info it.Value
                              (class_info.Name, class_info))
        |> Map.ofSeq


[<Sealed>]
type SemanticStage private (_ast: Ast.Program,
                            _class_info_map: Map<Ast.TYPE_NAME, ClassInfo>,
                            _diags: DiagnosticBag) =
    
    
    member private _.Translate(): string =
        ""
        
    
    static member Translate(ast: Ast.Program, diags: DiagnosticBag): string =
        let class_info_map = ClassInfos.collect ast
        let sema = SemanticStage(ast, class_info_map, diags)
        let asm = sema.Translate()
        asm

namespace LibCool.SemanticParts


open LibCool.AstParts


module Sema =


    type AttrInfo =
        { Name: Ast.ID
          Type: Ast.TYPE_NAME
          Index: int }


    type ParamInfo =
        { Name: Ast.ID
          Type: Ast.TYPE_NAME
          Index: int }


    type MethodInfo =
        { Name: Ast.ID
          ReturnType: Ast.TYPE_NAME
          Params: ParamInfo[]
          Index: int }


    type ClassInfo =
        { Name: string
          Attributes: Map<string, AttrInfo>
          Methods: Map<string, MethodInfo> }

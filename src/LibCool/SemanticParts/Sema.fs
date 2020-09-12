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
          Params: ParamInfo[]
          ReturnType: Ast.TYPE_NAME
          Override: bool
          Index: int }


    type ClassInfo =
        { Name: Ast.TYPE_NAME
          Attributes: Map<Ast.ID, AttrInfo>
          Methods: Map<Ast.ID, MethodInfo> }

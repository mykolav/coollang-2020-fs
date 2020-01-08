namespace LibCool.Tests.Parser

open System
open System.Text
open LibCool.Frontend
open LibCool.SourceParts

[<Sealed>]
type CoolRenderer private () =
    inherit AstListener()
    
    let _acc_cool_text = StringBuilder()
    
    override this.EnterClass(klass: ClassDecl, key: Guid, span: HalfOpenRange) : unit =
        _acc_cool_text.Append("class ") |> ignore
        
    override this.EnterVarFormals(var_formals: Node<VarFormal>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    override this.LeaveVarFormals(var_formals: Node<VarFormal>[]) : unit =
        _acc_cool_text.Append(")") |> ignore
        
    override this.EnterExtends(extends:Extends, key:Guid, span:HalfOpenRange) : unit =
        _acc_cool_text.Append(" extends ") |> ignore
        
    override this.EnterActuals(actuals: Node<Expr>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    override this.LeaveActuals(actuals: Node<Expr>[]) : unit =
        _acc_cool_text.Append(")") |> ignore
        
    override this.VisitID(id:ID, _:Guid, _:HalfOpenRange) : unit =
        _acc_cool_text.Append(id.Value) |> ignore 
    
    override this.VisitTYPE_NAME(type_name:TYPE_NAME, _:Guid, _:HalfOpenRange) : unit =
        _acc_cool_text.Append(type_name.Value) |> ignore

    override this.VisitCOLON() : unit =
        _acc_cool_text.Append(": ") |> ignore

    override this.VisitEQUALS() : unit =
        _acc_cool_text.Append(" = ") |> ignore

    override this.VisitNATIVE() : unit =
        _acc_cool_text.Append("native") |> ignore

    override this.VisitARROW() : unit =
        _acc_cool_text.Append(" => ") |> ignore
        
    override this.ToString() =
        _acc_cool_text.ToString()

    static member Render(ast: Ast) =
        let renderer = CoolRenderer()
        AstWalker.Walk(ast, renderer)
        renderer.ToString()

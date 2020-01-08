namespace LibCool.Tests.Parser

open System
open System.Text
open LibCool.Frontend
open LibCool.SourceParts

[<Sealed>]
type CoolRenderer private () as this =
    inherit AstVisitor()
    
    let _acc_cool_text = StringBuilder()
    
    override this.VisitClass(klass: ClassDecl, key: Guid, span: HalfOpenRange) : unit =
        _acc_cool_text.Append("class ") |> ignore
        this.WalkClass(klass, key, span)
        
    override this.VisitVarFormals(var_formals: Node<VarFormal>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
        this.WalkVarFormals(var_formals)
        _acc_cool_text.Append(")") |> ignore
        
    override this.VisitExtends(extends:Extends, key:Guid, span:HalfOpenRange) : unit =
        _acc_cool_text.Append(" extends ") |> ignore
        this.WalkExtends(extends, key, span)
        
    override this.VisitActuals(actuals: Node<Expr>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
        this.WalkActuals(actuals)
        _acc_cool_text.Append(")") |> ignore
        
    override this.VisitNATIVE() : unit =
        _acc_cool_text.Append("native") |> ignore

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
        renderer.VisitAst(ast)
        renderer.ToString()

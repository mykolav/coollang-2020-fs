namespace rec LibCool.SemanticParts


type Reg = Reg of int
    with
    static member Null = Reg -1


type RegisterSetItem =
    { mutable IsFree: bool
      Name: string }


[<Sealed>]
type RegisterSet() =


    let _regs: RegisterSetItem[] = [|
        (* 0 *) { IsFree=true; Name="%rbx" }
        (* 1 *) { IsFree=true; Name="%r10" }
        (* 2 *) { IsFree=true; Name="%r11" }
        (* 3 *) { IsFree=true; Name="%r12" }
        (* 4 *) { IsFree=true; Name="%r13" }
        (* 5 *) { IsFree=true; Name="%r14" }
        (* 6 *) { IsFree=true; Name="%r15" }
    |]
    
    
    member this.Allocate(): Reg =
        let index_opt = _regs |> Seq.tryFindIndex (fun it -> it.IsFree)
        match index_opt with
        | Some index ->
            _regs.[index].IsFree <- false
            Reg index
        | None -> invalidOp "Out of registers"
        
        
    member this.Free(reg: Reg): unit =
        if reg <> Reg.Null
        then
            let (Reg index) = reg
            let item = _regs.[index]
            if item.IsFree
            then
                invalidOp (sprintf "The register %i '%s' has not been allocated" index item.Name)
            
            item.IsFree <- true
        
        
    member this.NameOf(reg: Reg): string =
        let (Reg index) = reg
        _regs.[index].Name

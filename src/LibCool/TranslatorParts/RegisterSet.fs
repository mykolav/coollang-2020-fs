namespace rec LibCool.TranslatorParts


type Reg = Reg of int
    with
    static member Null = Reg -1


type RegisterSetItem =
    { mutable IsFree: bool
      mutable Owner: string
      Name: string }


[<Sealed>]
type RegisterSet() =


    let _regs: RegisterSetItem[] = [|
        (* 0 *) { IsFree=true; Owner=""; Name="%rbx" }
        (* 1 *) { IsFree=true; Owner=""; Name="%r10" }
        (* 2 *) { IsFree=true; Owner=""; Name="%r11" }
        (* 3 *) { IsFree=true; Owner=""; Name="%r12" }
        (* 4 *) { IsFree=true; Owner=""; Name="%r13" }
        (* 5 *) { IsFree=true; Owner=""; Name="%r14" }
        (* 6 *) { IsFree=true; Owner=""; Name="%r15" }
    |]
    
    
    member this.Count: int = _regs.Length
    
    
    member this.FreeCount: int =
        _regs |> Array.where (fun it -> it.IsFree) |> Array.length
        
        
    member this.AssertAllFree(): unit =
        if this.FreeCount <> this.Count
        then
            invalidOp "A register leak detected"
    
    
    member this.Allocate(owner: string): Reg =
        let index_opt = _regs |> Seq.tryFindIndex (fun it -> it.IsFree)
        match index_opt with
        | Some index ->
            let item = _regs.[index]
            item.IsFree <- false
            item.Owner <- owner
            Reg index
        | None -> invalidOp "Out of registers"
        
        
    member this.Free(reg: Reg): unit =
        if reg <> Reg.Null
        then
            let (Reg index) = reg
            let item = _regs.[index]
            if item.IsFree
            then
                invalidOp $"The register %i{index} '%s{item.Name}' has not been allocated"
            
            item.IsFree <- true
            item.Owner <- ""
            
            
    member this.IsAllocated(reg: string): bool =
        let item = _regs |> Seq.find (fun it -> it.Name = reg)
        not item.IsFree
        
        
    member this.NameOf(reg: Reg): string =
        if reg = Reg.Null
        then
            // TODO: Actually, we do want to raise an exception if Reg.Null is supplied!
            // TODO: This is a temporary measure, so that the exception doesn't distract
            // TODO: from type-checking code development.
            ""
        else
            
        let (Reg index) = reg
        _regs.[index].Name

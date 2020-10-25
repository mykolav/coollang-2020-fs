namespace rec LibCool.SemanticParts


open System.Collections.Generic
open LibCool.AstParts
open LibCool.SemanticParts


[<Sealed>]
type TypeComparer(_class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>) =


    member private this.Resolve(typename: TYPENAME): ClassSymbol =
        _class_sym_map.[typename]


    member this.Conforms(ancestor: ClassSymbol, descendant: ClassSymbol): bool =
        if ancestor.Is(BasicClasses.Any)
        then
            true
        else
            
        if descendant.Is(BasicClasses.Nothing)
        then
            true
        else
        
        if descendant.Is(BasicClasses.Null)
        then
            not (
                ancestor.Is(BasicClasses.Boolean) ||
                ancestor.Is(BasicClasses.Int) ||
                ancestor.Is(BasicClasses.Unit))
        else
        
        let rec conforms (descendant: ClassSymbol): bool =
            if descendant.Name = BasicClasses.Any.Name
            then
                false
            else
            
            if ancestor.Name = descendant.Name
            then
                true
            else
                
            conforms (this.Resolve(descendant.Super))
            
        conforms descendant
        
        
    member this.LeastUpperBound(type1: ClassSymbol, type2: ClassSymbol): ClassSymbol =
        if this.Conforms(ancestor=type1, descendant=type2)
        then type1
        else this.LeastUpperBound(type1=this.Resolve(type1.Super), type2=type2)
        
        
    // member this.LeastUpperBound(types: seq<ClassSymbol>): ClassSymbol =
    //     let rec least_upper_bound types =
    //         match List.ofSeq types with
    //         | [] -> invalidOp "types.Length = 0"
    //         | [ t1 ] -> t1
    //         | t1::t2::types_tl -> least_upper_bound (this.LeastUpperBound(t1, t2)::types_tl)
    //         
    //     least_upper_bound (List.ofSeq types)
        
        
    member this.LeastUpperBound(types: ClassSymbol[]): ClassSymbol =
        if types.Length = 0
        then
            invalidOp "types.Length = 0"
            
        let mutable least_upper_bound = types.[0]
        let mutable i = 1
        while i < types.Length do
            least_upper_bound <- this.LeastUpperBound(least_upper_bound, types.[i])
            i <- i + 1
            
        least_upper_bound

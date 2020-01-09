namespace LibCool.Frontend

open System
open LibCool.SourceParts

// ERROR HANDLING:
// 1) If any lexical errors, report and stop
// 2) If any syntax errors, report and stop
// 3) Semantic analysis should also get performed in stages,
//    see Eric Lippert's corresponding post for inspiration.
//    E.g.: detecting circular base class dependencies should be its own stage?
//    ...
//
// SYNTAX ERROR HANDLING:
// If a syntax error
//   a) Keep parsing other parts of the syntax node
//      as to diagnose as many syntax errors in one go as possible 
//   b) Evaluate to None until "bubble up" to a syntax nodes collection
//      or a syntax node with an optional child 
// ... ERRYYY: File.cool:LL:CC:  An incomplete feature ...
//
// ... ERRZZZ: File.cool:LL:CC: An incomplete var declaration
//     NOTE: 'foo' was not expected at this point of the var declaration
//     NOTE: Assuming 'foo' is the next expression's beginning
//     (If 'foo' doesn't match any expression's beginning,
//        a) Skip to the first token matching any relevant syntax node 
//        b) Report an the skipped tokens as invalid)
//
// ... ERRZZZ: File.cool:LL:CC: An incomplete var declaration
//     NOTE: 'var' was not expected at this point of the var declaration
//     NOTE: Assuming 'var' is the next var declaration's begging 

module Test =
    let test() =
        ()



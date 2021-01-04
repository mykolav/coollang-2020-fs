namespace LibCool.ParserParts


open LibCool.SourceParts
open System.Diagnostics
open System.Runtime.CompilerServices


(*
10 Lexical Structure
    The lexical units of Cool are integers, type identifiers, object 
    identifiers, special notation, strings, comments, keywords, 
    and white space.

10.1 Integers, Identifiers, and Special Notation
    Integer literals are non-empty strings of digits 0-9 not starting with 
    zero, except that 0 is a valid integer literal. Identifiers are strings 
    (other than keywords) consisting of letters, digits, and the underscore 
    character. 
    Type identifiers begin with a capital letter; 
    object identifiers begin with a lower case letter.

10.2 Strings
    There are two forms for string literals. 
    The simple version of string literals are enclosed in double quotes "...".
    Within a string literal, a sequence '\c' is illegal unless it is one of 
    the following:
        \0 NUL
        \b backspace
        \t tab
        \n newline
        \r return
        \f form feed
        \” double quote
        \\ backslash
    A newline character may not appear in the simple form of a string literal
    (even if escaped):
        "This is not\
        OK"

    The other form of string literals starts with three double quotes and 
    continues until ended by three double quotes (again). Any characters, 
    including backslashes and newlines are legal. The only thing forbidden
    is three double quotes. Thus the following represents a string literal:

    """This starts a string literal
    that continues on for several lines
    even though it includes "'s and \'s and newline characters
    in a "wild" profu\sion\\ of normally i\\egal t"ings.\"""

    The string contains literally everything inside of it.

    10.3 Comments
    There is two forms for comments in Cool. Any characters after two slashes
    // and before the next newline (or EOF, if there is no next newline) are 
    ignored. Also, any characters excepting the sequence */ maybe enclosed in 
    C-style comments: /*...*/.

    10.4 Keywords
    The keywords of Cool are 
    case class def else extends false if match native
    new null override super this true var while 
    and must be written in lowercase. The keyword native may only be used 
    in the special basic.cool file provided with the compiler. 
    Additionally, the following words are reserved for "Extended Cool" and 
    cannot be used: 
    abstract catch do final finally for forSome implicit import lazy object 
    package private protected requires return sealed throw trait try type val 
    with yield. (We call these the “illegal keywords.”)

    10.5 White Space
    White space consists of any sequence of the ASCII characters: 
    HT, NL, CR, and SP. These characters are represented by the following C/C++
    character literals respectively: '\t', '\n', '\r', and ' '.
*)


[<RequireQualifiedAccess>]
type TokenKind =
    | Invalid
    | EOF
    | Id of string
    // Literals
    | IntLiteral of int
    | StringLiteral of string
    | TripleQuotedStringLiteral of string
    // Punctuators
    | Plus
    | Minus
    | Slash
    | Star
    | Equal
    | EqualEqual
    | EqualGreater
    | Less
    | Greater
    | LessEqual
    | GreaterEqual
    | Exclaim
    | ExclaimEqual
    | LParen // (
    | RParen // )
    | LSquare // [
    | RSquare // ]
    | LBrace // {
    | RBrace // }
    | Colon
    | Semi
    | Dot
    | Comma
    // Keywords
    | KwCase 
    | KwClass 
    | KwDef 
    | KwElse 
    | KwExtends 
    | KwFalse 
    | KwIf 
    | KwMatch 
    | KwNative
    | KwNew 
    | KwNull 
    | KwOverride 
    | KwSuper 
    | KwThis 
    | KwTrue 
    | KwVar 
    | KwWhile 
    // Illegal/reserved keywords
    | KwAbstract 
    | KwCatch 
    | KwDo 
    | KwFinal 
    | KwFinally 
    | KwFor 
    | KwForSome 
    | KwImplicit 
    | KwImport 
    | KwLazy 
    | KwObject 
    | KwPackage 
    | KwPrivate 
    | KwProtected 
    | KwRequires
    | KwReturn
    | KwSealed 
    | KwThrow 
    | KwTrait 
    | KwTry 
    | KwType 
    | KwVal 
    | KwWith 
    | KwYield


[<DebuggerDisplay("K: [{Kind}] - S: [{Span}]")>]
[<IsReadOnly; Struct>]
type Token =
    { Kind: TokenKind
      Span: Span }
    with
    
    
    static member EOF(offset) = {
        Kind = TokenKind.EOF
        // EOF is the end of file, which means its size is 0.
        Span = Span.Of(offset, offset)
    }
    static member Invalid(span)= { Kind = TokenKind.Invalid; Span = span }
    static member Of(kind, span) = { Kind = kind; Span = span }
    
    
    member this.Is(kind: TokenKind): bool = this.Kind = kind
    member this.IsEof: bool = this.Kind = TokenKind.EOF
    
    
    member this.IsId: bool =
        match this.Kind with
        | TokenKind.Id _ -> true
        | _ -> false

    
    member this.IsInt: bool =
        match this.Kind with
        | TokenKind.IntLiteral _ -> true
        | _ -> false


    member this.IsString: bool =
        match this.Kind with
        | TokenKind.StringLiteral _ -> true
        | _ -> false


    member this.IsQqqString: bool =
        match this.Kind with
        | TokenKind.TripleQuotedStringLiteral _ -> true
        | _ -> false
    
    
    member this.IsKw: bool =
        match this.Kind with
        | TokenKind.KwCase 
        | TokenKind.KwClass 
        | TokenKind.KwDef 
        | TokenKind.KwElse 
        | TokenKind.KwExtends 
        | TokenKind.KwFalse 
        | TokenKind.KwIf 
        | TokenKind.KwMatch 
        | TokenKind.KwNative
        | TokenKind.KwNew 
        | TokenKind.KwNull 
        | TokenKind.KwOverride 
        | TokenKind.KwSuper 
        | TokenKind.KwThis 
        | TokenKind.KwTrue 
        | TokenKind.KwVar 
        | TokenKind.KwWhile -> true
        | _ -> false
    
    
    member this.IsReservedKw: bool =
        match this.Kind with
        | TokenKind.KwAbstract 
        | TokenKind.KwCatch 
        | TokenKind.KwDo 
        | TokenKind.KwFinal 
        | TokenKind.KwFinally 
        | TokenKind.KwFor 
        | TokenKind.KwForSome 
        | TokenKind.KwImplicit 
        | TokenKind.KwImport 
        | TokenKind.KwLazy 
        | TokenKind.KwObject 
        | TokenKind.KwPackage 
        | TokenKind.KwPrivate 
        | TokenKind.KwProtected 
        | TokenKind.KwRequires
        | TokenKind.KwReturn
        | TokenKind.KwSealed 
        | TokenKind.KwThrow 
        | TokenKind.KwTrait 
        | TokenKind.KwTry 
        | TokenKind.KwType 
        | TokenKind.KwVal 
        | TokenKind.KwWith 
        | TokenKind.KwYield -> true
        | _                 -> false
    
    
    member this.Id: string =
        match this.Kind with
        | TokenKind.Id value -> value
        | _ -> invalidArg "token" "The token is not an identifier"
    
    
    member this.Int: int =
        match this.Kind with
        | TokenKind.IntLiteral value -> value
        | _ -> invalidArg "token" "The token is not an int literal"
    
    
    member this.String: string =
        match this.Kind with
        | TokenKind.StringLiteral value -> value
        | _ -> invalidArg "token" "The token is not a string literal"
    
    
    member this.QqqString: string =
        match this.Kind with
        | TokenKind.TripleQuotedStringLiteral value -> value
        | _ -> invalidArg "token" "The token is not a \"\"\" string literal"

    

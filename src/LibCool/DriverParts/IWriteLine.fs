namespace LibCool.DriverParts


type IWriteLine =
    abstract member WriteLine: line:string -> unit
    
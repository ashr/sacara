﻿/// This simple program produce a text output with VM opcodes. 
/// The generated opcode are saved in the apprppriate src directory.
namespace GeneratePreBuildInfo

open System
open System.Collections.Generic
open System.Text
open ES.Sacara.Ir.Assembler
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open System.IO
open System.Reflection
open ES.Sacara.Ir.Assembler.Instructions

module Program =
    let generateOpCodes() =
        let opCodesBytes = new HashSet<Int32>()
        let opCodes = new Dictionary<String, VmOpCodeItem>()
        let rnd = new Random()
        
        FSharpType.GetUnionCases(typeof<VmOpCodes>)
        |> Array.iter(fun case ->            
            opCodes.Add(case.Name, new VmOpCodeItem(Name=case.Name))
            for j=1 to rnd.Next(2, 6) do
                let opCode = rnd.Next(10, 65535)
                if opCodesBytes.Add(opCode) then
                    opCodes.[case.Name].Bytes.Add(opCode)
        )
        opCodes

    let saveOpCodeInAssemblerDir(opCodes: Dictionary<String, VmOpCodeItem>) =
        let opCodeJson = JsonConvert.SerializeObject(opCodes.Values, Formatting.Indented)
        
        // copy file
        let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let assemblerSrcFile = Path.Combine(curDir, "..", "..", "..", "ES.Sacara.Ir.Assembler", "vm_opcodes.json")
        File.WriteAllText(assemblerSrcFile, opCodeJson)
        Console.WriteLine("Files copied to: " + assemblerSrcFile)

    let convertToDword(word32: Int32) =
        let word16 = UInt16.Parse(word32.ToString())
        let wordBytes = BitConverter.GetBytes(word16) |> Array.rev
        let wordString = String.Join(String.Empty, wordBytes |> Seq.map(fun b -> b.ToString("X").PadLeft(2, '0')))
        String.Format("0{0}h", wordString)

    let xorOpCode(length: Int32) (word32: Int32) =
        let word16 = uint16 word32
        let length16 = uint16 length
        let result = ((word16 ^^^ uint16 0xB5) + length16)
        int32 result

    let saveOpCodeInVmDir(opCodes: Dictionary<String, VmOpCodeItem>) =
        let sb = new StringBuilder()
        sb.AppendLine("; This file is auto generated, don't modify it") |> ignore

        let rnd = new Random()
        let generateMarker() = uint32(rnd.Next(0, 0xFFFF) <<< 18 ||| rnd.Next(0, 0xFFFF)).ToString("X").PadLeft(8, '0')
        let marker1 = generateMarker()
        let marker2 = generateMarker()
        sb.AppendFormat("marker1 EQU 0{0}h", marker1).AppendLine() |> ignore
        sb.AppendFormat("marker2 EQU 0{0}h", marker2).AppendLine().AppendLine() |> ignore
        
        opCodes
        |> Seq.map(fun kv -> kv.Value)
        |> Seq.iter(fun opCode ->
            let bytes = String.Join(", ", opCode.Bytes |> Seq.map(xorOpCode opCode.Bytes.Count) |> Seq.map convertToDword)
            let realBytes = String.Join(", ", opCode.Bytes |> Seq.map convertToDword)

            sb.AppendFormat(
                "; real opcodes: {1}{0}header_{2} EQU <DWORD 0{3}h, 0{4}h, {5}h, {6}>{0}", 
                Environment.NewLine,
                realBytes,
                opCode.Name,
                marker1, 
                marker2, 
                opCode.Bytes.Count, 
                bytes
                
            ).AppendLine() |> ignore
        )

        // write end marker
        sb.AppendFormat("header_end EQU <DWORD 0{0}h, 0{1}h>", marker2, marker1).AppendLine() |> ignore
        
        // copy file
        let fileContent = sb.ToString()
        let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let vmSrcFile = Path.Combine(curDir, "..", "..", "..", "SacaraVm", "vm_instructions_headers.inc")
        File.WriteAllText(vmSrcFile, fileContent)
        Console.WriteLine("Files copied to: " + vmSrcFile)

    let private ror(operand: UInt32) =
        let n = 6
        (operand >>> n) ||| (operand <<< (32-n))

    let private hashString(name: String) =
        let mutable hash = uint32 0
        name.ToUpperInvariant()
        |> Seq.iter(fun c ->
            let h1 = (hash + uint32 c) * uint32 1024
            let h2 = ror h1
            hash <- h1 ^^^ h2
        )
        hash

    let computeStringHashes() =
        let sb = new StringBuilder()
        sb.AppendLine("; This file is auto generated, don't modify it") |> ignore

        [
            // module names
            "kernel32.dll"
            "ntdll.dll"

            // function names
            "GetProcessHeap"
            "RtlAllocateHeap"
            "RtlFreeHeap"
        ] 
        |> List.map(fun name -> (name, hashString(name)))
        |> List.iter(fun (name, hash) ->
            let cleanName = name.Replace('.', '_')
            sb.AppendFormat("hash_{0} EQU 0{1}h", cleanName, hash.ToString("X")).AppendLine() |> ignore
        )

        // copy file
        let fileContent = sb.ToString()
        let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let vmSrcFile = Path.Combine(curDir, "..", "..", "..", "SacaraVm", "strings.inc")
        File.WriteAllText(vmSrcFile, fileContent)
        Console.WriteLine("Files copied to: " + vmSrcFile)

    [<EntryPoint>]
    let main argv =         
        computeStringHashes()

        let opCodes = generateOpCodes()        
        saveOpCodeInAssemblerDir(opCodes)
        saveOpCodeInVmDir(opCodes)
        0

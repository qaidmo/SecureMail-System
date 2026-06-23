/*
    SecureMail YARA Rules — Suspicious Office Macros
    Detects VBA macro patterns commonly used in phishing documents
    (Word .docm, Excel .xlsm, etc.).
*/

rule OfficeMacroAutoOpen
{
    meta:
        description = "Detects Office documents with auto-executing macros (AutoOpen/Auto_Open)"
        severity = "HIGH"
        author = "SecureMail Team"

    strings:
        $auto1 = "AutoOpen" nocase
        $auto2 = "Auto_Open" nocase
        $auto3 = "Workbook_Open" nocase
        $auto4 = "Document_Open" nocase
        $auto5 = "AutoExec" nocase
        $vba_marker = "VBAProject" nocase

    condition:
        $vba_marker and any of ($auto1, $auto2, $auto3, $auto4, $auto5)
}

rule OfficeMacroShellExec
{
    meta:
        description = "Detects Office macros that execute shell commands"
        severity = "CRITICAL"
        author = "SecureMail Team"

    strings:
        $shell1 = "Shell" nocase
        $shell2 = "WScript.Shell" nocase
        $shell3 = "cmd.exe" nocase
        $shell4 = "powershell" nocase
        $vba_marker = "VBAProject" nocase

    condition:
        $vba_marker and 2 of ($shell1, $shell2, $shell3, $shell4)
}

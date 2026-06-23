/*
    SecureMail YARA Rules — Phishing Document Indicators
    Detects patterns commonly found in phishing PDF/HTML attachments
    used to steal credentials.
*/

rule PhishingPDFFormAction
{
    meta:
        description = "Detects PDF files with embedded form submission actions (credential harvesting)"
        severity = "HIGH"
        author = "SecureMail Team"

    strings:
        $pdf_magic = "%PDF" at 0
        $action1 = "/SubmitForm" nocase
        $action2 = "/URI" nocase
        $action3 = "/OpenAction" nocase
        $action4 = "/JavaScript" nocase

    condition:
        $pdf_magic and 2 of ($action1, $action2, $action3, $action4)
}

rule PhishingHTMLForm
{
    meta:
        description = "Detects HTML files with password input fields (credential phishing pages)"
        severity = "HIGH"
        author = "SecureMail Team"

    strings:
        $html_tag = "<html" nocase
        $pass_input = "type=\"password\"" nocase
        $form_action = "<form" nocase
        $login_text = "login" nocase
        $signin_text = "sign in" nocase

    condition:
        $html_tag and $pass_input and $form_action and ($login_text or $signin_text)
}

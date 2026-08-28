dismissinator-slot-id = ID card
dismissinator-slot-paper = blank
dismissinator-slot-stamp = rubber stamp

dismissinator-no-id = No ID card inserted!
dismissinator-no-authority = The inserted ID card is not authorized to alter access!
dismissinator-no-paper = No blank inserted!
dismissinator-no-stamp = No rubber stamp inserted!

dismissinator-hit-popup = You have been dismissed!
dismissinator-outranked = Denied: the target is cleared for access your card does not carry!
dismissinator-unknown = UNKNOWN
dismissinator-access-none = none

dismissinator-document-text =
    ⠀[head=3]Nanotrasen[/head]
    ⠀[head=3]NOTICE OF DISMISSAL[/head]
    =============================================
    Station: { $station }
    Shift time and date: { $date }

    Employee: { $name }
    Position: { $job }

    By the present notice the above employee is relieved of their duties.
    All station access has been revoked.

    Revoked access: { $access }

    Issued by: { $authorName }
    Position: { $authorJob }
    =============================================

dismissinator-verb-toggle-mode = Switch mode
dismissinator-mode-dismissal = dismissal
dismissinator-mode-expansion = access expansion
dismissinator-mode-switched = Mode: { $mode }.
dismissinator-examine-mode = Set to [color=#6ec0ea]{ $mode }[/color].
dismissinator-expansion-popup = Your clearance has been expanded!

dismissinator-expansion-document-text =
    ⠀[head=3]Nanotrasen[/head]
    ⠀[head=3]NOTICE OF CLEARANCE EXPANSION[/head]
    =============================================
    Station: { $station }
    Shift time and date: { $date }

    Employee: { $name }
    Position: { $job }

    By the present notice the above employee is granted additional station clearance.

    Granted access: { $access }

    Issued by: { $authorName }
    Position: { $authorJob }
    =============================================

dismissinator-mode-objective = recruitment
dismissinator-objective-popup = You have been given a new assignment!

dismissinator-objectives-none = none on file
dismissinator-objective-document-text =
    ⠀[head=3]Nanotrasen[/head]
    ⠀[head=3]ASSIGNMENT ORDER[/head]
    =============================================
    Station: { $station }
    Shift time and date: { $date }

    Employee: { $name }

    The above employee is hereby assigned the following, effective immediately:

    { $objectives }

    =============================================
    ⠀[italic]This order supersedes all standing instructions.[/italic]

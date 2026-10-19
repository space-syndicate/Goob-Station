bank-program-name = Bank

bank-ui-no-account = Insert an ID card with a bank account.
bank-ui-owner = { $name } ({ $job })
bank-ui-number = Account #{ $number }
bank-ui-balance = { $balance } { $balance ->
    [one] credit
   *[other] credits
}
bank-ui-transfer-number = Account number
bank-ui-transfer-amount = Amount
bank-ui-transfer-button = Send
bank-ui-history-empty = No transactions yet.
bank-ui-income = +{ $amount }
bank-ui-expense = -{ $amount }

bank-transaction-starting = Starting funds
bank-transaction-salary = Salary
bank-transaction-transfer-in = Transfer from { $party }
bank-transaction-transfer-out = Transfer to { $party }

bank-notification-income = +{ $amount } { $amount ->
    [one] credit
   *[other] credits
}: { $reason }

bank-transfer-no-card = There is no ID card with a bank account in the PDA.
bank-transfer-invalid-amount = Invalid transfer amount.
bank-transfer-invalid-account = No account with this number was found.
bank-transfer-insufficient = Insufficient funds.
bank-transfer-success = Transferred { $amount } { $amount ->
    [one] credit
   *[other] credits
} to { $name }.

bank-card-examine-number = Bank account number: [color=yellow]{ $number }[/color].

bank-transaction-purchase = Purchase: { $party }

vending-paid-price = { $price } cr.
vending-paid-no-account = An ID card with a bank account is required to pay.
vending-paid-insufficient = Insufficient funds, { $price } cr. required.
vending-paid-purchased = Charged { $price } cr.

bank-transaction-bonus = Bonus from { $party }
bank-transaction-fine = Fine from { $party }
bank-transaction-with-comment = { $reason } ({ $comment })
bank-notification-charge = -{ $amount } { $amount ->
    [one] credit
   *[other] credits
}: { $reason }

bank-ui-manage-comment = Reason
bank-ui-manage-bonus = Bonus
bank-ui-manage-fine = Fine

bank-manage-denied = Your ID card is not authorized to issue bonuses and fines.
bank-manage-no-comment = Specify a reason.
bank-manage-no-station = No connection to the station budget.
bank-manage-no-budget = The department budget has insufficient funds.
bank-manage-bonus-success = Bonus of { $amount } cr. issued to { $name }.
bank-manage-fine-success = Fine of { $amount } cr. issued to { $name }.

bank-ui-section-account = ACCOUNT
bank-ui-section-transfer = TRANSFER
bank-ui-section-manage = BONUSES AND FINES
bank-ui-section-history = TRANSACTION HISTORY
bank-ui-credits = { $amount } cr.
bank-ui-manage-budget = Budget
bank-ui-manage-budget-balance = Budget balance
bank-ui-manage-bonus-left = Bonuses left
bank-ui-manage-max-fine = Fines up to
vending-paid-restricted = This item is sold only to staff authorized to carry it.
vending-paid-restricted-label = no access
cargo-console-withdraw-disabled = Central Command has prohibited withdrawing the station budget as cash. Reward crew with bonuses instead.
ptl-interact-budget = PTL revenue transferred to the station budget.
ptl-interact-no-budget = No connection to the station budget.

bank-transaction-fine-revoked = Fine revoked: { $party }
bank-ui-manage-unlimited = unlimited
bank-ui-manage-requests = Awaiting approval
bank-ui-manage-fines = Recent fines
bank-ui-manage-approve = Approve
bank-ui-manage-reject = Reject
bank-ui-manage-revoke = Revoke fine
bank-ui-manage-record-bonus = Bonus of { $amount } cr. for { $target }
bank-ui-manage-record-fine = Fine of { $amount } cr. for { $target }
bank-ui-manage-record-details = By { $issuer }, { $budget }: { $comment }

bank-manage-request-sent = The amount exceeds your limit. The request has been sent to the most senior head on shift for approval.
bank-manage-request-approved = Request approved.
bank-manage-request-rejected = Request rejected.
bank-manage-record-missing = Record not found, it may have already been handled.
bank-manage-fine-revoked = Fine revoked, the money was returned to the offender.

bank-notification-request-approved = Your request of { $amount } cr. for { $target } was approved.
bank-notification-request-rejected = Your request of { $amount } cr. for { $target } was rejected.

bank-radio-bonus = Bonus of { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }
bank-radio-bonus-approved = Bonus of { $amount } cr. for { $target } from { $issuer }, approved by { $manager }. Reason: { $comment }
bank-radio-fine = Fine of { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }
bank-radio-fine-approved = Fine of { $amount } cr. for { $target } from { $issuer }, approved by { $manager }. Reason: { $comment }
bank-radio-fine-revoked = Fine of { $amount } cr. for { $target } revoked by { $manager }.
bank-radio-request-bonus = Over-limit bonus request: { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }. Approval required in the "Bank" program.
bank-radio-request-fine = Oversized fine request: { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }. Approval required in the "Bank" program.
bank-radio-request-rejected-bonus = Bonus request of { $amount } cr. for { $target } from { $issuer } rejected by { $manager }.
bank-radio-request-rejected-fine = Fine request of { $amount } cr. for { $target } from { $issuer } rejected by { $manager }.
bank-manage-not-approver = Requests are currently approved by the { $job }.
bank-manage-no-approver = There is no one on shift who can approve this request.

bank-party-central-command = CentCom
bank-transaction-reverted = Transaction reverted by { $party }
bank-transaction-adjusted = Balance corrected by { $party }
bank-admin-verb-panel = CentCom Bank
bank-admin-title = CentCom Bank
bank-admin-search = Search by name, job or number
bank-admin-refresh = Refresh
bank-admin-tab-account = Account
bank-admin-tab-requests = Requests
bank-admin-tab-requests-count = Requests ({ $count })
bank-admin-tab-fines = Fines
bank-admin-no-account = Select an account on the left.
bank-admin-no-job = no job
bank-admin-no-requests = No requests awaiting approval.
bank-admin-no-fines = No fines yet.
bank-admin-account-row = { $name }, { $job } (#{ $number }): { $balance } cr.
bank-admin-account-title = { $name }, account #{ $number }
bank-admin-amount = Amount
bank-admin-comment = Reason (required)
bank-admin-bonus = CentCom bonus
bank-admin-fine = CentCom fine
bank-admin-set-balance = Set balance
bank-admin-revert = Revert
bank-admin-revert-hint = To revert a transaction, enter a reason in the field above.
cmd-bankpanel-desc = Opens the CentCom Bank panel for managing bank accounts.
cmd-bankpanel-help = Usage: bankpanel [account number]
cmd-bankpanel-hint = [account number]
bank-ui-open-account = Open an account
bank-open-account-success = Account #{ $number } has been opened on your ID card.
bank-open-account-failed = Could not open an account: an ID card without an existing account is required.
bank-admin-verb-zone-grid-off = Economy: disable on this grid
bank-admin-verb-zone-grid-on = Economy: enable on this grid
bank-admin-verb-zone-map-off = Economy: disable on this map
bank-admin-verb-zone-map-on = Economy: enable on this map
bank-offline = The station banking system is offline.

bank-transaction-chip-withdraw = Holochip withdrawal
holochip-verb-withdraw = Withdraw credits
holochip-dialog-amount = Amount
holochip-withdrawn = Withdrew { $amount } cr. as a holochip.
holochip-free-zone = The banking system does not work here.
stack-holochip = holochip
bank-transaction-chip-deposit = Holochip deposit
holochip-deposited = Deposited { $amount } cr. from the holochip.

bank-transaction-atm-withdraw = Cash withdrawal
bank-transaction-atm-deposit = Cash deposit
atm-ui-no-account = An ID card with a bank account is required.
atm-ui-amount = Amount
atm-ui-withdraw = Withdraw
atm-ui-deposit-hint = Insert cash into the ATM to deposit it.
atm-no-account = The ATM could not find an ID card with a bank account on you.
atm-deposited = Deposited { $amount } { $amount ->
    [one] credit
   *[other] credits
}.
atm-free-zone = This ATM does not serve this sector.

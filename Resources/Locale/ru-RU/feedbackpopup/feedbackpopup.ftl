feedbackpopup-window-name = Запросы обратной связи

feedbackpopup-control-button-text = Открыть ссылку

feedbackpopup-control-total-surveys = {$num ->
    [one] {$num} запрос
   *[other] {$num} запросов
}
feedbackpopup-control-no-entries= Нет запросов
feedbackpopup-control-ui-footer = Расскажите нам что вы думаете!

# Command strings
command-description-openfeedbackpopup = Открывает окно обратной связи.
command-description-feedback-show = Открывает окно обратной связи выбранным сессиям.
command-description-feedback-add = Добавляет прототип обратной связи выбранным клиентам и открывает им окно обратной связи если у него не было данного прототипа.
command-description-feedback-remove = Удаляет прототип обратной связи с выбранных клиентов.

feedbackpopup-give-command-name = givefeedbackpopup
feedbackpopup-show-command-name = showfeedbackpopup
cmd-givefeedbackpopup-desc = Gives the targeted player a feedback popup.
cmd-givefeedbackpopup-help = Usage: givefeedbackpopup <playerUid> <prototypeId>
cmd-showfeedbackpopup-desc = Open the feedback popup window.
cmd-showfeedbackpopup-help = Usage: showfeedbackpopup
feedbackpopup-command-error-invalid-proto = Invalid feedback popup prototype.
feedbackpopup-command-error-popup-send-fail = Failed to send popup! There probably isn't a mind attached to the given entity.
feedbackpopup-command-success = Sent popup!
feedbackpopup-command-hint-playerUid = <playerUid>
feedbackpopup-command-hint-protoId = <prototypeId>



$(document).ready(function () {

    $('#OS_Adyen_cmdSave').unbind("click");
    $('#OS_Adyen_cmdSave').click(function () {
        $('.processing').show();
        $('.actionbuttonwrapper').hide();
        // lower case cmd must match ajax provider ref.
        nbxget('os_adyen_savesettings', '.OS_Adyendata', '.OS_Adyenreturnmsg');
    });

    $('.selectlang').unbind("click");
    $(".selectlang").click(function () {
        $('.editlanguage').hide();
        $('.actionbuttonwrapper').hide();
        $('.processing').show();
        $("#nextlang").val($(this).attr("editlang"));
        // lower case cmd must match ajax provider ref.
        nbxget('os_adyen_selectlang', '.OS_Adyendata', '.OS_Adyendata');
    });


    $(document).on("nbxgetcompleted", OS_Adyen_nbxgetCompleted); // assign a completed event for the ajax calls

    // function to do actions after an ajax call has been made.
    function OS_Adyen_nbxgetCompleted(e) {

        $('.processing').hide();
        $('.actionbuttonwrapper').show();
        $('.editlanguage').show();

        if (e.cmd == 'os_adyen_selectlang') {
                        
        }

    };

});


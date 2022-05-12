// Document.all.elements functions
function absoluteLeft(obj) {
    var x = obj.offsetLeft;
    var parent = obj.offsetParent;
	while (parent != document.body && parent != null) {
	    x += parent.offsetLeft;
	    parent = parent.offsetParent;
	}
    return x;
}

function absoluteTop(obj) {
    var y = obj.offsetTop;
    var parent = obj.offsetParent;
	while (parent != document.body && parent != null) {
	    y += parent.offsetTop;
	    parent = parent.offsetParent;
	}
    return y;
}

function openModal(url, width, height) {
    if (width == undefined) width = 400;
    if (height == undefined) height = 400;

    var divModalContainer = document.getElementById("divModalContainer");
    if (divModalContainer == null) {
        divModalContainer = document.createElement('div');
        divModalContainer.id = "divModalContainer";
        document.getElementsByTagName('BODY')[0].appendChild(divModalContainer);
    }
    //var scroll = document.documentElement.scrollTop || document.body.scrollTop;
    //divModalContainer.style.top = scroll + divWaitContainerTop + 'px';
    //divModalContainer.style.left = Math.floor(document.body.clientWidth / 2) - (divWaitContainerWidth / 2) + 'px';
    divModalContainer.className = 'modalDialog';
    divModalContainer.style.width = "100%";
    divModalContainer.style.height = "100%";
    divModalContainer.style.display = "";
    
    var html = "";
    ajaxSend("Post", url, "", function() {
        html = "<div style='background-color: #f0f0f0; border:1px solid silver; width:" + width + "px; height:" + height + "px;'>";
        html += "<table border='0'>";
        html += "<tr><td style='text-align:right; padding-bottom: 12px;'><img src='../Content/Img/ClosePreview.png' style='width:16px; heigth:16px; cursor:pointer;' onClick='closeModal()'></td>";
        html += "<tr><td>";
        html += this.responseText;
        html += "</td></tr></table>";
        html += "</div>";

        divModalContainer.innerHTML = html;
    });
}

function closeModal(modalContainerId) {
    if (modalContainerId == undefined)
        modalContainerId = "divModalContainer";

    var divModalContainer = document.getElementById(modalContainerId); // "divModalContainer"
    if (divModalContainer != null) {
        divModalContainer.style.display = "none";
    }
}

// функция центрирования  
function alignCenter(elem) {
    elem.css({
        left: ($jQuery(window).width() - elem.width()) / 2 + 'px', // получаем координату центра по ширине      
        top: ($jQuery(window).height() - elem.height()) / 2 + 'px' // получаем координату центра по высоте    
    });
}


function closeModalImgOver() {
    document.getElementById("modalBox_Img_close").src = "../Content/Img/DlgCloseHover.png";
}
function closeModalImgOut() {
    document.getElementById("modalBox_Img_close").src = "../Content/Img/DlgClose.png";
}

// Static Grid Head on visible page
var gridTrHeadTopInit = null;
var gridTrHeadLeftInit = null;
function gridHeadOnScroll(gridId) {
    var grid = document.getElementById(gridId);
    var tr = grid.firstChild.firstChild;   //trHead        
    if (gridTrHeadTopInit == null)
        gridTrHeadTopInit = tr.style.top;
    if (gridTrHeadLeftInit == null)
        gridTrHeadLeftInit = tr.style.left;

    var trTop = tr.offsetTop;
    var trScroll = tr.offsetParent.scrollTop;

    if (trScroll >= trTop) {

        tr.style.position = "absolute";
        tr.style.top = tr.offsetParent.scrollTop;
        tr.style.left = 1;
        //top:expression(this.offsetParent.scrollTop + this.offsetParent.offsetHeight - this.offsetHeight
    }
    else {
        tr.style.position = "relative";
        tr.style.top = gridTrHeadTopInit;
        tr.style.left = gridTrHeadLeftInit;
    }
}

function getFormatDate(date, formatMode)
{
    var d = date.getDate();
    var m = date.getMonth() + 1;
    var y = date.getFullYear();

    if (formatMode == "yyyymmdd") // 20160515
        return y + (m.toString().length === 1 ? ("0" + m) : m) + (d.toString().length === 1 ? ("0" + d) : d);
    else if (formatMode == "dd.mm.yyyy") // 15.05.2016
        return (d.toString().length === 1 ? ("0" + d) : d) + "." + (m.toString().length === 1 ? ("0" + m) : m) + "." + y;
    else                // 15.05.2016
        return (d.toString().length === 1 ? ("0" + d) : d) + "." + (m.toString().length === 1 ? ("0" + m) : m) + "." + y;
}


function waitDiv(caption) {
    var mbContainer = document.getElementById("divMbContainer");
    if (mbContainer == undefined) {
        mbContainer = document.createElement('div');
        mbContainer.id = "divMbContainer";
        document.forms[0].appendChild(mbContainer);
    }

    //Panel for Loading ModalBox....
    if (caption == undefined)
        caption = "Ждите... выполняется загрузка...";
    mbContainer.innerHTML = "<div style='text-align:center;background-color:#f0f0f0; color:navy; font-weight:bold; padding: 12px; font-size:8pt; border:1px solid dimgray;'><img src='/img/blue-loading.gif'><br/>" + caption + "</div>";
    mbContainer.style.width = "200px";
    mbContainer.style.height = "100px";
    mbContainer.style.verticalAlign = "middle";
    mbContainer.style.position = "absolute";
    var scroll = document.documentElement.scrollTop || document.body.scrollTop;
    mbContainer.style.top = scroll + 300 + 'px';
    mbContainer.style.left = Math.floor(document.body.clientWidth / 2) - 100 + 'px';

    return mbContainer;
}


// Ajax.js
// Variables
var requestLimitTimeout = 35000;    // 30 cек.
var divWaitContainer = null;
var divWaitContainerWidth = 200;
var divWaitContainerHeight = 70;
var divWaitContainerTop = 260;
var waitContainerTitle = "Ждите... Выполняется загрузка";
var asyncInProgress = false;    // Flag async processing. if true, then another process not allowed
var divContextMenuContainer = null; // Div для отображения контекстного меню

// Show ajax waiting div 
function showWaitContainer(caption) {
    var parent = document.getElementsByTagName('BODY')[0];

    if (divWaitContainer == null || divWaitContainer == undefined) {
        divWaitContainer = document.createElement('div');
        parent.appendChild(divWaitContainer);
    }

    var scroll = document.documentElement.scrollTop || document.body.scrollTop;
    divWaitContainer.style.top = scroll + divWaitContainerTop + 'px';
    divWaitContainer.style.left = Math.floor(document.body.clientWidth / 2) - (divWaitContainerWidth / 2) + 'px';
    divWaitContainer.innerHTML = "<img src='/Img/blue-loading.gif'><div class='divInnerWaitContainer'>" + (caption == undefined ? waitContainerTitle : caption) + '</div>';
    divWaitContainer.className = 'divWaitContainer';
    divWaitContainer.style.width = divWaitContainerWidth;
    divWaitContainer.style.height = divWaitContainerHeight;
    
}

// Hide and delete Ajax WaitContainer
function hideWaitContainer() {
    if (divWaitContainer != null || divWaitContainer != undefined) {
        var parent = document.getElementsByTagName('BODY')[0];
        parent.removeChild(divWaitContainer);
        divWaitContainer = null;
    }
}

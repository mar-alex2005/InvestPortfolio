// Core JS: js framework

var global = (function() {
    return this;
}());


function gridSort(orderByColumn) {
    if (typeof window.refreshGrid !== "function") {
        alert("Ошибка.\nНа текущей странице (либо в глобальном контексте) не определена функция-обработчик события refreshGrid().\nДанная функция необходима для обновления данных в гриде."); return;
    }

    window["refreshGrid"].call(null, orderByColumn);
}


var util = (function() {
    var dwContainer = null, // ссылка на DivWait контейнер
        dwContainerTitle = "Ждите... Выполняется загрузка.",
        dwContainerWidth = 200,
        dwContainerHeight = 70,
        dwContainerTop = 260,
        defaultGridRowCheckPrefix = "rowCheck_",
        trNormalBg,
        winVar,
        //For Resizeable textarea control elements
        textareaResizeable = null,
        textareaStaticOffset = null,
        useEventListener = (typeof window.addEventListener === "function"), // W3C
        useAttachEvent = (typeof document.attachEvent === "function")       // IE
        ;

    // частные методы
    function helper() { alert("private"); }

    // реализация необязательной процедуры инициализации
    // ...

    // общедоступные члены
    return {

        //getById
        getById: function(id) { return document.getElementById(id); },
        
        //getTick
        getTick: function() { return new Date().valueOf(); },
        
        //swaiting div 
        showWait: function(caption) {
            var bodyElement = document.body; //document.getElementByTagName('BODY')[0];
            var scroll = document.documentElement.scrollTop || document.body.scrollTop;

            if (dwContainer == null) {
                dwContainer = document.createElement('DIV');
                bodyElement.appendChild(dwContainer);
            }
            dwContainer.style.top = scroll + dwContainerTop + 'px';
            dwContainer.style.left = Math.floor(bodyElement.clientWidth / 2) - (dwContainerWidth / 2) + 'px';
            dwContainer.innerHTML = "<img src='/Img/blue-loading.gif'><div class='divInnerWaitContainer'>" + (caption == undefined ? dwContainerTitle : caption) + '</div>';
            dwContainer.className = 'divWaitContainer';
            dwContainer.style.width = dwContainerWidth;
            dwContainer.style.height = dwContainerHeight;
        },

        hideWait: function() {
            if (dwContainer != null)
                document.body.removeChild(dwContainer);
            dwContainer = null;
        },
        
        //значения элементов фильтра
        getFilterValues: function(filterPanelId) {
            var params = {},
                items = document.getElementById(filterPanelId).getElementsByTagName("*"),
                i, len;

            for (i = 0, len = items.length; i < len; i++) {
                if (items[i].tagName == "INPUT") {
                    if (items[i].type == "text" || items[i].type == "hidden")
                        params[items[i].id] = items[i].value;
                    else if (items[i].type == "checkbox")
                        params[items[i].id] = items[i].checked;
                }
                if (items[i].tagName == "SELECT") {
                    if (items[i].selectedIndex >= 0)
                        params[items[i].id] = items[i].value;
                    else
                        params[items[i].id] = "";
                }
            }
            return params;
        },

        // open new html window with params
        // default param = "width=600,height=500,top=10,left=10,scrollbars=1,resizable=1,status=1,toolbar=1"
        openWindow: function(name, url, param) {
            if (param == "" || param == undefined)
                param = "width=600,height=500,top=10,left=10,scrollbars=1,resizable=1,status=1,toolbar=1";
            param = param.toLowerCase();

            var topIndex = -1,
                leftIndex = -1,
                heightIndex = -1,
                widthIndex = -1,
                aPar = param.split(","),
                height = 300, 
                width;

            // выделяем из параметров высоту (если нет, то 300)
            for (n = 0; n < aPar.length; n++)
                if (aPar[n].indexOf("height") >= 0)
                    heightIndex = n;

            if (heightIndex >= 0)
                height = parseInt(aPar[heightIndex].split("=")[1]);

            //корректируем высоту открываемого окна на 
            //1) высоту заголовка окна;
            //2) высоту statusbar окна (если есть);
            //3) высоту toolbar окна (если есть);
            //4) высоту menubar окна (если есть);
            //5) высоту location строки окна (если есть)
            var totalHeight = height + 20;

            for (n = 0; n < aPar.length; n++) {
                if (aPar[n].indexOf("status=1") >= 0)
                    totalHeight = totalHeight + 25; //высота statusbar по умолчанию
                if (aPar[n].indexOf("toolbar=1") >= 0)
                    totalHeight = totalHeight + 25; //высота toolbar по умолчанию
                if (aPar[n].indexOf("menubar=1") >= 0)
                    totalHeight = totalHeight + 25; //высота menubar по умолчанию
                if (aPar[n].indexOf("location=1") >= 0)
                    totalHeight = totalHeight + 25; //высота toolbar по умолчанию
            }

            // выделяем из параметров ширину (если нет, то 300)
            for (n = 0; n < aPar.length; n++) if (aPar[n].indexOf("width") >= 0) widthIndex = n;
            if (widthIndex >= 0) width = parseInt(aPar[widthIndex].split("=")[1]);
            else width = 300;

            // формируем строковые значения
            var topValue = "top=" + (screen.availHeight - totalHeight) / 2;
            var leftValue = "left=" + (screen.availWidth - width) / 2;

            if (topValue < 0)
                topValue = 0;

            if (leftValue < 0)
                leftValue = 0;

            // ищем индекс вхождения top,left,height,width
            for (n = 0; n < aPar.length; n++) {
                if (aPar[n].indexOf("top") >= 0) topIndex = n;
                if (aPar[n].indexOf("left") >= 0) leftIndex = n;
                if (aPar[n].indexOf("height") >= 0) heightIndex = n;
                if (aPar[n].indexOf("width") >= 0) widthIndex = n;
            }

            // изменяем top
            if (topIndex >= 0) aPar[topIndex] = topValue;
            else {
                aPar.length++;
                aPar[aPar.length - 1] = topValue;
            }
            // изменяем left
            if (leftIndex >= 0) aPar[leftIndex] = leftValue;
            else {
                aPar.length++;
                aPar[aPar.length - 1] = leftValue;
            }
            // изменяем width
            if (widthIndex >= 0) aPar[widthIndex] = "width=" + width;
            else {
                aPar.length++;
                aPar[aPar.length - 1] = "width=" + width;
            }
            // изменяем height
            if (heightIndex >= 0) aPar[heightIndex] = "height=" + height;
            else {
                aPar.length++;
                aPar[aPar.length - 1] = "height=" + height;
            }

            // собираем строку параметров
            var par = "";
            for (var n = 0; n < aPar.length; n++) par += "," + aPar[n];
            par = par.substr(1);

            if (!winVar) {
                winVar = window.open(url, name, par);
            } else {
                if (winVar) {
                    winVar = window.open(url, name, par);
                    winVar.focus();
                }
            }
            return winVar;
        },
        
        // обновление текущего окна
        refreshWindow: function() {
            window.location.reload();
        },

        windowMoveCenter: function() {
            window.moveTo((screen.width - document.body.clientWidth) / 2, (screen.height - document.body.clientHeight) / 2);
        },
        
        // Standard window with confirm
        confirmMsg: function(text) {
            if (window.confirm(text) == true)
                window.event.returnValue = true;
            else
                window.event.returnValue = false;

            return window.event.returnValue;
        },

        absoluteLeft: function (obj) {
            var x = obj.offsetLeft;
            var parent = obj.offsetParent;
            while (parent != document.body && parent != null) {
                x += parent.offsetLeft;
                parent = parent.offsetParent;
            }
            return x;
        },

        absoluteTop: function (obj) {
            var y = obj.offsetTop;
            var parent = obj.offsetParent;
            while (parent != document.body && parent != null) {
                y += parent.offsetTop;
                parent = parent.offsetParent;
            }
            return y;
        },
        
        // Get shorty format of date (format 12.06.2010)
        getShortDate: function(d) {
            var mm = d.getMonth() + 1;
            return (d.getDate().length == 1 ? ("0" + d.getDate()) : d.getDate()) + '.' + (mm.toString().length == 1 ? ("0" + mm) : mm) + '.' + d.getFullYear();
        },

        // Get selected valkuew from Html SELECT element
        getSelectValue: function(objId) {
            var cb = document.getElementById(objId);
            return cb.value;
        },

        // For resizeable control element
        startDrag: function(sender) {
            textareaResizeable = util.getById(sender.getAttribute("textareaId"));
            textareaStaticOffset = textareaResizeable.clientHeight - window.event.y;

            //textarea.css('opacity', 0.25);
            textareaResizeable.style.opacity = "0.25";
            textareaResizeable.style.filter = "alpha(Style=0, Opacity=25);"; // для прозрачности фона

            //$jQuery(document).mousemove(performDrag).mouseup(endDrag);
            document.onmousemove = util.performDrag;
            document.onmouseup = util.endDrag;
            return false;
        },
        performDrag: function() {
            //resizeableTextarea.height(Math.max(32, staticOffset + window.event.y) + 'px');            
            textareaResizeable.style.height = (Math.max(32, textareaStaticOffset + window.event.y) + 'px');
            return false;
        },
        endDrag: function() {
            //$jQuery(document).unbind("mousemove", performDrag).unbind("mouseup", endDrag);
            document.onmousemove = null;
            document.onmouseup = null;
            textareaResizeable.style.opacity = "1"; //resizeableTextarea.css('opacity', 1);
            textareaResizeable.style.filter = "alpha(Style=0, Opacity=100);"; // для прозрачности фона
        },
        
        hideModalBox: function() {
            var container = util.getById("divModalContainer");
            if (container != undefined) {
                container.innerHTML = "";
                container.style.display = "none";
            }
        },
        
        // Интерфес для работыт с событиями элементов (инициализируется дополнительно)
        addListener: function(el, type, fn) {
            if (useEventListener) 
                el.addEventListener(type, fn, false);
            else
                el.attachEvent("on" + type, fn);
        },
        removeListener: function(el, type, fn) {
            if (useEventListener) 
                el.removeEventListener(type, fn, false);
            else
                el.detachEvent("on" + type, fn);
        },
        
        gridOnClick: function(e) {
            e = e || window.event;  // получить объект события и элемент-источник
            //var src = e.target || e.srcElement;

            if (typeof window["localGridOnClick"] === "function") {
                window["localGridOnClick"](e);
                util.stopEvents(e);
            }
        },
        
        stopEvents: function(e) {
            // предотвратить дальнейшее всплытие события
            if (typeof e.stopPropagation === "function") {
                e.stopPropagation();
            }
            if (typeof e.cancelBubble !== "undefined") {
                e.cancelBubble = true;
            }
            // предотвратить выполнение действия по умолчанию
            if (typeof e.preventDefault === "function") {
                e.preventDefault();
            }
            if (typeof e.returnValue !== "undefined") {
                e.returnValue = false;
            }
        },
        
        // Добавление пустой первой строки в выпадающий список (SELECT)
        addSelectNullOption: function(cbObj, value, text) {
            if (value == undefined) value = '';
            if (text == undefined) text = '';
            var option = document.createElement('option');
            option.value = value;
            option.text = text;
            cbObj.options.add(option);
        },

        // Добавление строки (OPTION) в выпадающий список (SELECT)
        addSelectOption: function (cbObj, value, text) {
            var option = document.createElement('option');
            option.value = value;
            option.text = text;
            cbObj.options.add(option);
        }
    };
}());
// инициализация    
//util.init();


var ax = (function () {
    // локальные переменные
    //var var1 = null;

    // частные методы
    //function helper() { alert("private"); }

    // реализация необязательной процедуры инициализации
    // ...

    // общедоступные члены
    return {
        // AJAX request
        send: function(method, url, params, callbackFunc) {
            var xhr = new XMLHttpRequest();
            var isPost = (method.toLowerCase() === "post");
            var urlParams = "?IsAsyncRequest=1&noCache=" + util.getTick(); // Math.random

            for (var p in params) {
                if (!params.hasOwnProperty(p)) continue;
                urlParams += "&" + p + "=" + encodeURIComponent(params[p]);
            }

            console.log("urlParams: ", urlParams);

            xhr.open(method, (isPost ? url : url.concat(urlParams)), true);
            xhr.setRequestHeader('IsAsyncRequest', '1'); // признак асинхронного запроса
            xhr.setRequestHeader('Cache-Control', 'no-cache, no-store'); 
            xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
            //xhr.setRequestHeader('User-Agent', 'XMLHTTP/1.0');
            //xhr.timeout = 60000; // 15 секунд

            xhr.onreadystatechange = function() {
                if (xhr.readyState != 4) return; // запрос ещё не завершён
                if (xhr.status == 200) // Все ок
                {
                    if (typeof callbackFunc === "function") {
                        // this — ссылка на объект, в контексте которого вызвана функция
                        callbackFunc.call(xhr); //callbackFunc(xhr);                        
                    }
                }
                else {
                    console.log(xhr.responseText);
                    callbackFunc.call(xhr);
                    //alert("Произошла ошибка во время запроса к серверу:\nreadyState: " + xhr.readyState + "\nHTTP-статус ответа: " + xhr.status + ': ' + xhr.statusText + '\n\n' + xhr.responseText);
                }
                xhr = null; // по завершении запроса удаляем ссылку из замыкания
            };

            xhr.ontimeout = function() {
                alert('Произошла ошибка во время запроса к серверу: \nЗапрос превысил максимально допустимое время ожидания (15 сек.)');
                xhr = null;
            };

            xhr.send(isPost ? urlParams : null);
        },

      
        // Стандартное сообщение об ошибке
        showHandleError: function(xhr) {
            alert("Произошла ошибка во время запроса к серверу:\nreadyState: " + xhr.readyState + "\nHTTP-статус ответа: " + xhr.status + ': ' + xhr.statusText + '\n\n' + xhr.responseText);
        },

        // Расширенное сообщение об ошибке
        showExtHandleError: function(xhr) {
            
            var modalContainerId = "divExceptionModalContainer";
            var width = 700;
            var height = 400;

            var divMb = document.getElementById(modalContainerId);
            if (divMb == null) {
                divMb = document.createElement("div");
                divMb.id = modalContainerId;

                divMb.style.position = "absolute";
                divMb.classname = "ModalBackground"; // для прозрачности фона
                //divMb.style.backgroundColor = "#ffffff"; // для прозрачности фона
                //divMb.style.filter = "alpha(Style=0, Opacity=70);"; // для прозрачности фона
                //divMb.style.opacity = "0.7";  // для других браузеров
                divMb.style.backgroundImage = "url(../Content/Img/WhiteOpacity70.png)"; // делает фон прозрачным при помощи прозрачной картинки
                divMb.style.height = "100%";
                divMb.style.width = "100%";
                divMb.style.top = "0";
                divMb.style.left = "0";
                divMb.style.zIndex = "10";

                document.getElementsByTagName("BODY")[0].appendChild(divMb);
            }
            divMb.style.display = "";

            var top = $jQuery(document).scrollTop() + ($jQuery(window).height() / 2) - (height / 2);
            var left = $jQuery(window).width() / 2 - (width / 2);

            var html = "";
            
            html += "<TABLE id='modalBox_PW-1' style='DISPLAY:; Z-INDEX: 10000; width: " + width + "px; height:" + height + "px; VISIBILITY: ; POSITION: relative; TOP: " + top + "px; left: " + left + "px; BORDER-COLLAPSE: separate;' cellSpacing='0' cellPadding='0'>";
            html += "<TBODY>";
            html += "<tr>";
            html += "<TD class='dxpcControl' style='CURSOR: default; text-align: left; vertical-align: top; background-color:white; padding-right:6px; padding-top:14px;'>";
            html += "<div id='divModalClose' style='text-align:right'><div style='display:inline; padding:8px;' onMouseOver='closeModalImgOver()' onMouseOut='closeModalImgOut()'><IMG id='modalBox_Img_close' style='cursor:pointer; cursor:hand; BORDER-TOP-WIDTH: 0px; BORDER-LEFT-WIDTH: 0px; BORDER-BOTTOM-WIDTH: 0px; WIDTH: 15px; HEIGHT: 15px; BORDER-RIGHT-WIDTH: 0px' alt='Закрыть диалоговое окно' title='Закрыть диалоговое окно' src='../Content/Img/DlgClose.png' onclick=closeModal('" + modalContainerId + "') /></div></div>";
            html += "<div id='divModalContent' style='padding:24px; padding-top:0;'>";
                
            html += "Произошла ошибка во время асинх. запроса к серверу:<br/>readyState: " + xhr.readyState;
            html += "<br/>HTTP-статус ответа: " + xhr.status + ": " + xhr.statusText;
            html += "<br/><br/>";

            html += xhr.responseText;
                
            html += "</div>";
            html += "</TD>";
            html += "<TD style='BACKGROUND: url(../Content/Img/1_71.png) no-repeat left top'></TD>";
            html += "</TR>";
            html += "<TR>";
            html += "<TD style='BACKGROUND: url(../Content/Img/1_73.png) no-repeat left top'></TD>";
            html += "<TD valign='top' style='vertical-align: top;'><IMG style='BORDER-TOP-WIDTH: 0px; BORDER-LEFT-WIDTH: 0px; BORDER-BOTTOM-WIDTH: 0px; WIDTH: 5px; HEIGHT: 5px; BORDER-RIGHT-WIDTH: 0px; vertical-align: top;' alt='' src='../Content/Img/DX_1.png'></TD></TR></TBODY>";
            html += "</TABLE>";

            divMb.innerHTML = html;
        }
    };
}());
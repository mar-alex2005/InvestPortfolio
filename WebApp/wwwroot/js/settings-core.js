///////////////////////////////////////////////////////
//
// Функционал работы с локальными пользовательскими параметрами
//
///////////////////////////////////////////////////////

// Глобальный объект
var LocalSetting = {
    // Коллекция локальных пользовательских параметров
    list: [],

    prefixName: "None", // присвоить правильное значение

    // возвращает параметр в качестве объекта
    getSetting: function (name) {
        var len = LocalSetting.list.length, i;
        for (i = 0; i < len; i++) {
            if (LocalSetting.list[i].name === name)
                return LocalSetting.list[i];
        }
        console.warn("Не найден параметр (UserSetting) по имени '" + name + "'");
        return null;
    },

    // возвращает текущее локальное значение параметра (если оно есть) или его значение, заданное по умолчанию
    getValueOrDefault: function (name) {
        var len = LocalSetting.list.length, i;
        for (i = 0; i < len; i++) {
            var item = LocalSetting.list[i];
            if (item.name === name) {
                var type = this.getSetting(name).type;
                //console.log("type: ", type);

                var v;
                if (this.value(item.name) != null) {
                    if (type === "bool") {
                        v = (this.value(name) === "true");
                    }
                    else if (type === "int")
                        v = parseInt(this.value(name));
                    else if (type === "date") {
                        v = (this.value(name) != null) ? this.value(name) /*util.getFormatDate(this.value(name))*/ : "";
                    }
                    else if (type === "json")
                        v = (this.value(name) != null) ? JSON.stringify(this.value(name)) : "";
                    else if (type == "datetime") {
                        v = (this.value(name) != null && this.value(name) != "null") ? new Date(parseInt(this.value(name))) : null;
                    }
                    else
                        v = this.value(name);
                }

                //console.log("value: ", v);
                return this.value(item.name) != null ? v : item.defaultValue;
            }
        }
        console.warn("Не найден параметр (UserSetting) по имени '" + name + "'");
        return null;
    },

    // сохраняем значение на клиентсокм компе в объекте LocalStorage
    saveToStorage: function (name, value) {
        if (window["localStorage"] == null)
            return false;
        //console.log("save ", value);

        var v = value;
        var type = this.getSetting(name).type;

        if (type == null) // не удалось определить type
            return false;

        if (type === "bool")
            v = value;
        else if (type === "int")
            v = (value);
        else if (type === "date")
            v = (value != null) ? util.getFormatDate(value) : "";
        else if (type === "json")
            v = (value != null) ? JSON.stringify(value) : "";
        else if (type === "datetime")
            v = (value != null) ? value.getTime() : null;

        localStorage.setItem(this.prefixName + "." + name, v);
        //console.log("save [" + name + "] = " + LocalSetting.getValueOrDefault(name));

        return true;
    },

    // Получаем значение пользовательских настроек из LocalStorage по имени (вегда возвращает значение типа String)
    value: function (name) {
        if (window["localStorage"] == undefined || window["localStorage"] == null)
            return null;

        if (localStorage[this.prefixName + "." + name] == undefined)
            return null;

        //console.log("[this.prefixName + '.' + name + "] = " + localStorage.getItem("SubtitlesSetting." + name));
        return localStorage.getItem(this.prefixName + "." + name);
    },

    // Очищаем коллекцию пользовательских настроек в браузере
    clearStorageValues: function () {
        if (window["localStorage"] == undefined || window["localStorage"] == null)
            return;
        window["localStorage"].clear();
        console.log("localStorage cleared!");
    }
};
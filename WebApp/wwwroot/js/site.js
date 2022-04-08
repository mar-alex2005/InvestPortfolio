var gridColumns = [];
var curentPortfolio = null;

// Перечисление всех возможных локальных пользовательских настроек
var LocalSettingEnum = {
    AutoPlayClip: "AutoPlayClip",
    IsPlayerControlVisible: "IsPlayerControlVisible",
    SoundEnable: "SoundEnable"
};
Object.freeze(LocalSettingEnum);

// Глобальный объект: функционал работы с локальными пользовательскими параметрами
LocalSetting.prefixName = "InvSetting";
LocalSetting.list = [
    { name: LocalSettingEnum.AutoPlayClip, type: "bool", defaultValue: false },
    { name: LocalSettingEnum.IsPlayerControlVisible, type: "bool", defaultValue: false },
    { name: LocalSettingEnum.SoundEnable, type: "bool", defaultValue: false }
];

// Инициализация коллекции локальных параметров пользователя значениями по умолчанию
// Например, если параметры пустые, новый пользователь и т.д.
(function () {
    const len=LocalSetting.list.length;
    for (let i = 0; i < len; i++) {
        const item = LocalSetting.list[i];
        if (LocalSetting.value(item.name) == null && item.defaultValue != undefined) {
            LocalSetting.saveToStorage(item.name, item.defaultValue, item.type);
            console.log(`LocalUserSettings['${item.name}'] init with default value: ${item.defaultValue}`);
        }
    }
    console.info("LocalUserSettings init successfully...");
})();


$(document).ready(function () {
    console.log("document.ready()....");
    //let h = document.documentElement.clientHeight - absoluteTop(document.getElementById("divStocks")) - 6;
    //$("#divStocks").height(h);

    //$(".td-filter span").click(function (/*e*/) {
    //    refreshTickers(this.getAttribute("data"));
    //});

    //$(".tbl-g th").click(function (/*e*/) {
    //    console.log(this.getAttribute("col"));
    //    openStocks(params["country"], this.getAttribute("col"));
    //});
});

var tikers = {};


function init() {
    console.log("init...");
    gridColumns.push({ className: "tbl-profit", orderCol: "ticker", orderBy: "asc" });
    gridColumns.push({ className: "tbl-g", orderCol: "ticker", orderBy: "asc" });
}

function getColumnByTbl(className)
{
    for (let i = 0; i < gridColumns.length; i++) {
        if (gridColumns[i].className === className)
            return gridColumns[i];
    }

    return null;
}

function revertOrderBy(tblColumn)
{
    if (tblColumn.orderBy === "asc")
        tblColumn.orderBy = "desc";
    else 
        tblColumn.orderBy = "asc";
}

function openTickers(tickerId) {
	window.location = "/Home/TickerIndex?tickerId=" + tickerId;
}

function openStocks(country, orderBy) {
    var params = {};
    params["country"] = (country == undefined ? "" : country);
    params["orderBy"] = (orderBy == undefined ? "company" : orderBy);
    console.log(params);
    showWaitContainer();

    ax.send("Post", "/Home/Stocks", params, function() {
        document.getElementById("divGrid").innerHTML = this.responseText;
        hideWaitContainer();
        //util.hideModalBox();

        getStockChartData();

        $(document).ready(function () {
            //console.log("Объектная модель готова к использованию!");
            let h = document.documentElement.clientHeight - absoluteTop(document.getElementById("divStocks")) - 6;
            $("#divStocks").height(h);

            $(".td-filter span").click(function(/*e*/) {
                openStocks(this.getAttribute("data"));
            });

            $(".tbl-g th").click(function(/*e*/) {
                console.log(this.getAttribute("col"));
                openStocks(params["country"], this.getAttribute("col"));
            });
        });
    });
}

function openOperationsByTicker(tickerId)
{
    //const params = {tickerId: tickerId};
	window.location = "/Home/TickerIndex?tickerId=" + tickerId;

    //showWaitContainer();

    //ax.send("Get", "/Home/TickerIndex", params, function() {
    //    document.getElementById("divMain").innerHTML = this.responseText;
    //    hideWaitContainer();

    //    $(document).ready(function () {
    //        const h = document.documentElement.clientHeight - absoluteTop(document.getElementById("divTickerList")) - 6;
    //        $("#divTickerList").height(h);
    //    });
    //});
}

function openTickerDetails(tickerId) {
    const params = {tickerId: tickerId};

    showWaitContainer();

    ax.send("Get", "/Home/TickerDetails", params, function () {
        document.getElementById("divGrid").innerHTML = this.responseText;
        hideWaitContainer();  

        $(document).ready(function () {
            const h = document.documentElement.clientHeight - absoluteTop(document.getElementById("divGrid")) - 6;
            $("#divGrid").height(h);
        });
    });
}

function getPortfolioData(params) {
    params["isJson"] = true;

    ax.send("Post", "/Home/Portfolio", params, function () {
        //console.log("ss_", this.responseText);
        const list = eval(JSON.parse(this.responseText));
        var data = [];

        for (let i = 0; i < list.length; i++) {
            //console.log("ss", list[i]);
            data.push({
                ticker: list[i].stock.ticker, profitPercent: list[i].profitPercent, profitInRur: list[i].profitInRur,
                curStockSum: list[i].curStockSum
            });
        }
        //console.log("data", data);

        $(function () {
            $("#chartPortfolio").dxChart({
                dataSource: data,
                commonSeriesSettings: {
                    type: "bar",
                    argumentField: "ticker"
                },
                series: [{
                    type: "bar",
                    //barPadding: 0.1,
                    barWidth: 50,
                    valueField: "profitInRur"
                }],   

                argumentAxis: {
                    argumentField: "ticker",
                    font: {
                        size: 8
                    },
                    label: {
                        displayMode: "rotate", 
                        overlappingBehavior: "none",
                        rotationAngle: -45,
                        font: {
                            size: 10,
                            color: "blue",
                            family: "Segoe UI"
                        }
                    }
                },

                valueAxis: [{
                    allowDecimals: true,
                    axisDivisionFactor: 40,
                    visualRangeUpdateMode: "reset",
                    autoBreaksEnabled: false,
                    logarithmBase: 2,

                    grid: {
                        visible: true
                    },
                    title: {
                        text: "Profit In Rur"
                    },
                    label: {
                        overlappingBehavior: "none",
                        customizeText: function () {
                            return this.value + " р.";
                        },
                        font: {
                            size: 10
                        }
                    },
                    type: "continuous",
                    tick: {
                        color: "silver"
                    },
                    minorTick: {
                        visible: true
                    },
                    minorTickCount: 5
                }],

                legend: {
                    visible: false
                },

                customizePoint: function () {
                    if (this.value > 0) {
                        return { color: "green", hoverStyle: { color: "#ff7c7c" } };
                    }
                    else {
                        return { color: "red", hoverStyle: { color: "#8c8cff" } };
                    }
                },

                tooltip: {
                    enabled: true,
                    format: {
                        type: "fixedPoint",
                        precision: 2
                    },
                    customizeTooltip: function (arg) {
                        return {
                            text: "<b>" + arg.argumentText + "</b><br/>" + arg.valueText
                        };
                    }
                }
            });
        });

        $(function () {
            $("#piePortfolio").dxPieChart({
                type: "doughnut",
                palette: "Soft Pastel",
                dataSource: data,
                legend: {
                    position: "outside",
                    horizontalAlignment: "right", // or "left" | "right"
                    verticalAlignment: "top", // or "bottom"
                    font: {
                        size: 8
                    },
                    visible: true,
                    margin: 0
                },

                tooltip: {
                    enabled: true,
                    //format: "millions",
                    customizeTooltip: function (arg) {
                        return {
                            text: arg.valueText + " - " + (arg.percent * 100).toFixed(2) + "%"
                        };
                    }
                },

                "export": {
                    enabled: false
                },
                series: [{
                    argumentField: "ticker",
                    valueField: "curStockSum",
                    label: {
                        visible: true,
                        font: {
                            size: 9
                        },
                        connector: {
                            visible: true,
                            width: 0.5
                        },
                        position: "columns",
                        customizeText: function (arg) {
                            return arg.valueText + " (" + arg.percentText + ")";
                        }
                    }
                }]
            });
        });
    });
}


function openProfit(portfolio, isJson, column, orderBy) {
    var params = {};
    params["portfolio"] = (portfolio == undefined ? "" : portfolio);
    params["isJson"] = false;
    params["column"] = (column == undefined ? "" : column);
    params["orderBy"] = (orderBy == undefined ? "" : orderBy);
    console.log(params);

    curentPortfolio = params["portfolio"];

    showWaitContainer();

    ax.send("Post", "/Home/Profit", params, function () {
        document.getElementById("divMain").innerHTML = this.responseText;

        //getPortfolioData();

        hideWaitContainer();
        //util.hideModalBox();

        $(document).ready(function () {
            //console.log("Объектная модель готова к использованию!");
            //let h = document.documentElement.clientHeight - absoluteTop(document.getElementById("divStocks")) - 6;
            //$("#divStocks").height(h);

            $(".td-filter span").click(function (/*e*/) {
                openProfit(this.getAttribute("data"));
            });

            $(".tbl-profit th").click(function (/*e*/) {
                var tblColumn = getColumnByTbl("tbl-profit");
                revertOrderBy(tblColumn);

                console.log(this.getAttribute("col"));
                openProfit(curentPortfolio, isJson, this.getAttribute("col"), tblColumn.orderBy);
            });
        });
    });
}


function openPortfolio(portfolio, isJson, column, orderBy) {
    var params = {};
    params["portfolio"] = (portfolio == undefined ? "" : portfolio);
    params["isJson"] = false;
    params["column"] = (column == undefined ? "" : column);
    params["orderBy"] = (orderBy == undefined ? "" : orderBy);
    console.log(params);

    curentPortfolio = params["portfolio"];

    showWaitContainer();

    ax.send("Post", "/Home/Portfolio", params, function () {
        document.getElementById("divGrid").innerHTML = this.responseText;

        getPortfolioData(params);

        hideWaitContainer();
        //util.hideModalBox();

        $(document).ready(function () {
            //console.log("Объектная модель готова к использованию!");
            //let h = document.documentElement.clientHeight - absoluteTop(document.getElementById("divStocks")) - 6;
            //$("#divStocks").height(h);

            $(".tbl-g th").click(function (/*e*/) {
                var tblColumn = getColumnByTbl("tbl-g");
                revertOrderBy(tblColumn);

                console.log(this.getAttribute("col"));
                openPortfolio(curentPortfolio, isJson, this.getAttribute("col"), tblColumn.orderBy);
            });
        });
    });
}


function openCache() {
    const params = {};

    showWaitContainer();

    ax.send("Post", "/Home/Cache", params, function () {
        document.getElementById("divMain").innerHTML = this.responseText;

        getCacheInData();

        hideWaitContainer();

        $(document).ready(function () {
            //console.log("Объектная модель готова к использованию!");
            let h = document.documentElement.clientHeight - absoluteTop(document.getElementById("divOperCache")) - 10;
            $("#divOperCache").height(h);
        });
    });
}

function openDivs(cur) {
    if (cur == undefined)
        cur = null;

    const params = { cur: cur, year: null };

    const divYear = document.getElementById("tbDivYear");
    if (divYear != null) 
        params.year = divYear.value;

    showWaitContainer();

    ax.send("Post", "/Home/Divs", params, function () {
        document.getElementById("divMain").innerHTML = this.responseText;

        //getCacheInData();

        am4core.ready(function () {
            // Themes begin
            am4core.useTheme(am4themes_animated);

            // Create chart instance
            var chart = am4core.create("chartDivsUsd", am4charts.PieChart);
            loadChartDivs(2, chart);

            chart = am4core.create("chartDivsRur", am4charts.PieChart);
            loadChartDivs(1, chart);
        });

        hideWaitContainer();
    });
}


function openBonds(cur) {
	if (cur == undefined)
		cur = null;

	const params = { cur: cur, year: null };

	const divYear = document.getElementById("tbDivYear");
	if (divYear != null) 
		params.year = divYear.value;

	showWaitContainer();

	ax.send("Post", "/Home/Bonds", params, function () {
		document.getElementById("divMain").innerHTML = this.responseText;

		//getCacheInData();

		//am4core.ready(function () {
		//	// Themes begin
		//	am4core.useTheme(am4themes_animated);

		//	// Create chart instance
		//	var chart = am4core.create("chartDivsUsd", am4charts.PieChart);
		//	loadChartDivs(2, chart);

		//	chart = am4core.create("chartDivsRur", am4charts.PieChart);
		//	loadChartDivs(1, chart);
		//});

		hideWaitContainer();
	});
}


function openHist() {
    var params = {};

    showWaitContainer();

    ax.send("Post", "/Home/Hist", params, function () {
        document.getElementById("divMain").innerHTML = this.responseText;

        ax.send("Post", "/Home/Hist", {isJson: true}, function () {
            var list = eval(JSON.parse(this.responseText));
            var data = [];

            for (var i = list.length - 1; i >= 0; i--) {
                //console.log("ss1", list[i]);
                data.push({ date: list[i].date, profit: list[i].profit });
            }

            //console.log("data1", data);

            $(function() {
                $("#chartHistProfit").dxChart({
                    title: "Profit",
                    type: "spline",
                    dataSource: data,
                    commonSeriesSettings: {
                        argumentField: "date",
                        type: "bar",
                        hoverMode: "allArgumentPoints",
                        font: { size: 9 },
                        label: {
                            visible: true,
                            font: { size: 9 }
                        }
                    },
                    commonAxisSettings: {
                        grid: {
                            visible: true
                        }
                    },
                    series: [{
                        valueField: "profit",
                        name: "Profit",
                        color: "limegreen",
                        font: { size: 9 },
                        label: {
                            visible: true,
                            backgroundColor: "white",
                            font: { color: "black", size: 9 }
                        },
                    }],

                    margin: {
                        bottom: 10
                    },

                    legend: {
                        visible: false,
                        verticalAlignment: "top",
                        horizontalAlignment: "right"                        
                    },
                    "export": {
                        enabled: false
                    },
                    argumentAxis: {
                        label: {
                            format: {
                                type: "decimal"
                            }
                        },
                        allowDecimals: false,
                        axisDivisionFactor: 60
                    },
                    //tooltip: {
                    //    enabled: true,
                    //    location: "edge",
                    //    customizeTooltip: function (arg) {
                    //        return {
                    //            text: arg.valueText
                    //        };
                    //    }
                    //}
                });
            });
        });

        hideWaitContainer();
    });
}


function loadChartDivs(curId, chart) {
    const params = { curId: curId };

    ax.send("Post", "/Home/DivsTotalData", params, function() {
		console.log("::", this);
        const list = eval(JSON.parse(this.responseText));
        const data = [];
        console.log("list___:", list);

        for (let i = 0; i < list.length; i++) {
            //console.log("ticker:", list[i].stock.ticker);
            data.push({ ticker: list[i].stock.ticker, summa: list[i].sum });
        }
        chart.data = data;

        // Add and configure Series
        var pieSeries = chart.series.push(new am4charts.PieSeries());
        pieSeries.dataFields.value = "summa";
        pieSeries.dataFields.category = "ticker";
        pieSeries.slices.template.stroke = am4core.color("#fff");
        pieSeries.slices.template.strokeOpacity = 1;

        // This creates initial animation
        pieSeries.hiddenState.properties.opacity = 1;
        pieSeries.hiddenState.properties.endAngle = -90;
        pieSeries.hiddenState.properties.startAngle = -90;

        chart.hiddenState.properties.radius = am4core.percent(0);
        chart.radius = am4core.percent(70);

        //chart.legend.valueLabels.template.text = "{value.value}";
        pieSeries.labels.template.text = "{ticker}";  // "{ticker}: {value.value}";

        pieSeries.labels.template.disabled = true;
        pieSeries.ticks.template.disabled = true;

        var title = chart.titles.create();
        title.text = (curId === 1 ? "RUR" : "USD");
        title.fontSize = 12;        
    });
}


function loadPrices()
{
    ax.send("Post", "/Home/LoadPrices", null, function () {
        //var list = eval(JSON.parse(this.responseText));
        //var data = [];
    });
}

function getCacheInData() {
    ax.send("Post", "/Home/CacheInData", null, function () {
        var list = eval(JSON.parse(this.responseText));
        var data = [];

        for (var i = list.length-1; i >= 0; i--) {
            //console.log("ss1", list[i]);
            data.push({ period: list[i].period, summa: list[i].summa });
        }

        console.log("data11", data);

        $(function () {
            $("#chartCache").dxChart({
                rotated: true,
                dataSource: data, 
                commonSeriesSettings: {
                    argumentField: "period",
                    type: "bar",
                    hoverMode: "allArgumentPoints",
                    selectionMode: "allArgumentPoints",
                    font: { size: 9 },
                    label: {
                        visible: true,
                        format: {
                            type: "fixedPoint",
                            precision: 0
                        },
                        font: { size: 9 }
                    }
                },
                series: {
                    argumentField: "period",
                    valueField: "summa",
                    type: "bar",
                    color: "limegreen",
                    font: { size: 9 },  
                    label: {
                        visible: true,
                        backgroundColor: "white",
                        font: { color: "black", size: 9 }
                    },
                },

                legend: {
                    visible: false
                },
                "export": {
                    enabled: false
                },
                //tooltip: {
                //    enabled: true,
                //    location: "edge",
                //    customizeTooltip: function (arg) {
                //        return {
                //            text: arg.valueText
                //        };
                //    }
                //}
            });
        });

    });
}

function getStockChartData()
{
    ax.send("Post", "/Home/StockChartData", null, function () {
        var list = eval(JSON.parse(this.responseText));
        var data = [];

        for (var i = 0; i < list.length; i++) {
            //console.log("ss1", list[i]);
            data.push({ ticker: list[i].ticker, summa: list[i].data.sumBalance });
        }
        console.log("data1", data);

        $(function () {
            $("#pie").dxPieChart({
                palette: "bright",
                dataSource: data,
                legend: {
                    position: "outside",
                    horizontalAlignment: "right", // or "left" | "right"
                    verticalAlignment: "top" // or "bottom"
                },

                tooltip: {
                    enabled: true,
                    //format: "millions",
                    customizeTooltip: function (arg) {
                        return {
                            text: arg.argumentText + "<br/>" + arg.valueText
                        };
                    }
                },

                "export": {
                    enabled: false
                },
                series: [{
                    argumentField: "ticker",
                    valueField: "summa",
                    label: {
                        visible: true,
                        font: {
                            size: 9
                        },
                        connector: {
                            visible: true,
                            width: 0.5
                        },
                        position: "columns",
                        customizeText: function (arg) {
                            return arg.valueText + " (" + arg.percentText + ")";
                        }
                    }
                }]
            });
        });
    });
}


function refreshTickers() {

}

function showHidePos(accType, posNum, icon) {
	//console.debug(accType, posNum);
	$(icon).toggleClass("icon-minus-squared");
	$(icon).toggleClass("icon-plus-squared");

	var items = document.querySelectorAll(".tr-pos-oper[posNum='" + posNum + "'][accType='" + accType + "']");
	//console.debug(items);

	for (let i=0; i < items.length; ++i) {
		var tr = items[i];
		tr.style.display = tr.style.display === "" 
			? "none" 
			: "";
	}
	
}
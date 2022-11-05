class DivUi {

	params = null;

	constructor() {
		if (this.cur == undefined)
			this.cur = null;

		this.params = { cur: this.cur, year: null };

		const divYear = document.getElementById("tbDivYear");
		if (divYear != null) 
			this.params.year = divYear.value;
	}

	load() {
		var module = this;
		ax.send("Post", "/Home/Divs", this.params, function () {
			hideWaitContainer();
			document.querySelector(".body-content").innerHTML = this.responseText;
			module._onLoaded();
			//getCacheInData();

			hideWaitContainer();
		});
	}

	_onLoaded() {
		var module = this;
		am4core.ready(function () {
			// Themes begin
			am4core.useTheme(am4themes_animated);

			//	// Create chart instance
			//	var chart = am4core.create("chartDivsUsd", am4charts.PieChart);
			//	//loadChartDivs(2, chart);

			var chart = am4core.create("chartDivsRur", am4charts.PieChart);
			module._loadChartDivs(1, chart);
		});
	}

	_loadChartDivs(curId, chart) {
		ax.send("Post", "/Home/StockChartData", { cur: curId }, function () {
			var list = eval(JSON.parse(this.responseText));
			var data = [];

			for (let i = 0; i < list.length; i++) {
				console.log("ss1", list[i]);
				data.push({ ticker: list[i].stock.t, summa: list[i].sum });
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
}
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
			document.querySelector(".body-content").innerHTML = this.responseText;
			module._onLoaded();
			hideWaitContainer();
		});
	}

	_onLoaded() {
		this._loadChartDivs(1, "chartDivsRur");
		this._loadChartDivs(2, "chartDivsUsd");
	}

	_loadChartDivs(curId, chartId) {
		ax.send("Post", "/Home/StockChartData", { cur: curId }, function () {
			const list = eval(JSON.parse(this.responseText));
			const data = [];

			for (let i = 0; i < list.length; i++) {
				//console.log("ss1", list[i]);
				data.push({ ticker: list[i].stock.t, summa: list[i].sum });
			}
			//console.log("data1", data);

			am4core.useTheme(am4themes_animated);

			var chart = am4core.create(chartId, am4charts.PieChart);
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
}
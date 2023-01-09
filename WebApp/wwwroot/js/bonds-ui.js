class BondsUi {
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
		ax.send("Post", "/Home/Bonds", this.params, function () {
			document.querySelector(".body-content").innerHTML = this.responseText;
			module._onLoaded();
			hideWaitContainer();
		});
	}

	_onLoaded() {
		am4core.useTheme(am4themes_animated);
		var chart = am4core.create("chartBonds", am4charts.PieChart);
		this._loadChartBonds(1, chart);
	}

	_loadChartBonds(curId, chart) {
		const params = { curId: 1 };

		ax.send("Post", "/Api/Bonds", params, function() {
			const list = JSON.parse(this.responseText);
			const data = [];
			//console.log("list___:", list);

			for (let i = 0; i < list.length; i++) {
				//console.log("ticker:", list[i].stock.ticker);
				data.push({ ticker: list[i].company, summa: list[i].value });
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
			//title.text = (curId === 1 ? "RUR" : "USD");
			title.fontSize = 12;        
		});
	}
}
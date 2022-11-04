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

			//am4core.ready(function () {
			//	// Themes begin
			//	am4core.useTheme(am4themes_animated);

			//	// Create chart instance
			//	var chart = am4core.create("chartDivsUsd", am4charts.PieChart);
			//	//loadChartDivs(2, chart);

			//	chart = am4core.create("chartDivsRur", am4charts.PieChart);
			//	//loadChartDivs(1, chart);
			//});

			hideWaitContainer();
		});
	}

	_onLoaded() {
		
	}

}
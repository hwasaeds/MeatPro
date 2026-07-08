(() => {
	const sidebar = document.querySelector('.sidebar');
	const sidebarToggle = document.querySelector('[data-sidebar-toggle]');
	const sidebarCollapsedKey = 'meatpro.sidebar.collapsed';
	const sidebarScrollKey = 'meatpro.sidebar.scroll';

	const isMobile = () => window.innerWidth <= 992;

	if (sidebar) {
		const savedCollapsed = window.localStorage.getItem(sidebarCollapsedKey);
		if (savedCollapsed !== null && !isMobile()) {
			document.body.classList.toggle('sidebar-collapsed', savedCollapsed === 'true');
		}
		const savedScroll = window.sessionStorage.getItem(sidebarScrollKey);
		if (savedScroll !== null) {
			sidebar.scrollTop = parseInt(savedScroll, 10);
		}
	}

	if (sidebarToggle && sidebar) {
		sidebarToggle.addEventListener('click', () => {
			if (isMobile()) {
				sidebar.classList.toggle('sidebar-open');
				document.body.classList.toggle('sidebar-open');
			} else {
				const isCollapsed = !document.body.classList.contains('sidebar-collapsed');
				document.body.classList.toggle('sidebar-collapsed', isCollapsed);
				window.localStorage.setItem(sidebarCollapsedKey, String(isCollapsed));
			}
		});
	}

	window.addEventListener('resize', () => {
		if (!isMobile()) {
			sidebar?.classList.remove('sidebar-open');
			document.body.classList.remove('sidebar-open');
		}
	});

	if (sidebar) {
		sidebar.addEventListener('scroll', () => {
			window.sessionStorage.setItem(sidebarScrollKey, String(sidebar.scrollTop));
		});
		window.addEventListener('beforeunload', () => {
			window.sessionStorage.setItem(sidebarScrollKey, String(sidebar.scrollTop));
		});
	}

	const dashboard = window.meatProDashboard;
	if (!dashboard || typeof Chart === 'undefined') return;

	const baseOptions = {
		responsive: true,
		maintainAspectRatio: false,
		plugins: { legend: { labels: { usePointStyle: true, boxWidth: 8, padding: 16 } } }
	};

	const chartColors = {
		primary: '#8b0000',
		success: '#22c55e',
		warning: '#d4af37',
		danger: '#ef4444',
		info: '#06b6d4'
	};

	const charts = [
		{
			id: 'productionOutputChart',
			type: 'bar',
			labels: dashboard.production.labels,
			values: dashboard.production.values,
			label: 'Output',
			backgroundColor: chartColors.primary + '22',
			borderColor: chartColors.primary,
			borderWidth: 2,
			borderRadius: 4
		},
		{
			id: 'inventoryMovementChart',
			type: 'doughnut',
			labels: dashboard.inventory.labels,
			values: dashboard.inventory.values,
			label: 'Movements',
			backgroundColor: [chartColors.success + 'cc', chartColors.danger + 'cc', chartColors.warning + 'cc', chartColors.info + 'cc'],
			borderWidth: 0
		},
		{
			id: 'topProductsChart',
			type: 'bar',
			labels: dashboard.topProducts.labels,
			values: dashboard.topProducts.values,
			label: 'Units produced',
			backgroundColor: chartColors.success + '22',
			borderColor: chartColors.success,
			borderWidth: 2,
			borderRadius: 4,
			indexAxis: 'y'
		},
		{
			id: 'materialConsumptionChart',
			type: 'line',
			labels: dashboard.consumption.labels,
			values: dashboard.consumption.values,
			label: 'Consumed',
			backgroundColor: chartColors.warning + '22',
			borderColor: chartColors.warning,
			borderWidth: 2,
			fill: true,
			tension: 0.4,
			pointRadius: 3
		}
	];

	charts.forEach(chart => {
		const canvas = document.getElementById(chart.id);
		if (!canvas) return;

		const isHorizontal = chart.indexAxis === 'y';

		new Chart(canvas, {
			type: chart.type,
			data: {
				labels: chart.labels,
				datasets: [{
					label: chart.label,
					data: chart.values,
					backgroundColor: chart.backgroundColor || '#8b0000',
					borderColor: chart.borderColor || '#8b0000',
					borderWidth: chart.borderWidth ?? 0,
					fill: chart.fill ?? false,
					tension: chart.tension ?? 0,
					borderRadius: chart.borderRadius ?? 0,
				}]
			},
			options: {
				...baseOptions,
				indexAxis: isHorizontal ? 'y' : undefined,
				scales: isHorizontal ? {
					x: { beginAtZero: true, grid: { color: '#f1f5f9' } },
					y: { grid: { display: false } }
				} : {
					y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
					x: { grid: { display: false } }
				}
			}
		});
	});

})();

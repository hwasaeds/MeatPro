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
	if (!dashboard || typeof Chart === 'undefined') {
		return;
	}

	const baseOptions = {
		responsive: true,
		maintainAspectRatio: false,
		plugins: {
			legend: { labels: { usePointStyle: true } }
		}
	};

	const charts = [
		{
			id: 'productionOutputChart',
			type: 'line',
			labels: dashboard.production.labels,
			values: dashboard.production.values,
			label: 'Output',
			borderColor: '#8b0000',
			backgroundColor: 'rgba(139, 0, 0, 0.16)',
			fill: true,
			tension: 0.35
		},
		{
			id: 'inventoryMovementChart',
			type: 'bar',
			labels: dashboard.inventory.labels,
			values: dashboard.inventory.values,
			label: 'Movements',
			borderRadius: 12,
			backgroundColor: ['#8b0000', '#d4af37', '#6b7280', '#16a34a']
		},
		{
			id: 'topProductsChart',
			type: 'doughnut',
			labels: dashboard.topProducts.labels,
			values: dashboard.topProducts.values,
			label: 'Top products',
			backgroundColor: ['#8b0000', '#d4af37', '#1f2937', '#16a34a']
		},
		{
			id: 'materialConsumptionChart',
			type: 'radar',
			labels: dashboard.consumption.labels,
			values: dashboard.consumption.values,
			label: 'Consumption',
			borderColor: '#8b0000',
			backgroundColor: 'rgba(212, 175, 55, 0.18)'
		}
	];

	charts.forEach(chart => {
		const canvas = document.getElementById(chart.id);
		if (!canvas) {
			return;
		}

		new Chart(canvas, {
			type: chart.type,
			data: {
				labels: chart.labels,
				datasets: [{
					label: chart.label,
					data: chart.values,
					borderColor: chart.borderColor || '#8b0000',
					backgroundColor: chart.backgroundColor || 'rgba(139, 0, 0, 0.16)',
					fill: chart.fill ?? false,
					tension: chart.tension ?? 0,
					borderRadius: chart.borderRadius ?? 0,
				}]
			},
			options: baseOptions
		});
	});
})();

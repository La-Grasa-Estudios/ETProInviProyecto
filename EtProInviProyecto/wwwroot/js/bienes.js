let paginaActual = 1;
let filasPorPagina = 10;

async function actualizarTablaInventario(pagina = 1, resultadosPorPagina = 10) {
    const tbody = document.getElementById('inventoryTable');
    if (!tbody) return;

    const codigo = document.getElementById('filtroCodigo')?.value || '';
    const descripcion = document.getElementById('filtroDescripcion')?.value || '';
    const departamentoId = document.getElementById('filtroDepartamento')?.value || '';
    const grupo = document.getElementById('filtroGrupo')?.value || '';
    const subgrupo = document.getElementById('filtroSubgrupo')?.value || '';
    const seccion = document.getElementById('filtroSeccion')?.value || '';
    const responsableId = document.getElementById('filtroResponsable')?.value || '';
    const fechaDesde = document.getElementById('filtroFechaDesde')?.value || '';
    const fechaHasta = document.getElementById('filtroFechaHasta')?.value || '';

    let url = `/api/lista?pagina=${pagina}&resultadosPorPagina=${resultadosPorPagina}`;
    if (codigo) url += `&codigo=${encodeURIComponent(codigo)}`;
    if (descripcion) url += `&descripcion=${encodeURIComponent(descripcion)}`;
    if (departamentoId) url += `&departamentoId=${departamentoId}`;
    if (grupo) url += `&grupo=${encodeURIComponent(grupo)}`;
    if (subgrupo) url += `&subgrupo=${encodeURIComponent(subgrupo)}`;
    if (seccion) url += `&seccion=${encodeURIComponent(seccion)}`;
    if (responsableId) url += `&responsableId=${encodeURIComponent(responsableId)}`;
    if (fechaDesde) url += `&fechaDesde=${encodeURIComponent(fechaDesde)}`;
    if (fechaHasta) url += `&fechaHasta=${encodeURIComponent(fechaHasta)}`;

    try {
        const response = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' } });
        if (!response.ok) throw new Error(`Error ${response.statusText}`);

        const datos = await response.json();
        const items = datos.items || [];
        const totalRegistros = datos.total || 0;

        tbody.innerHTML = '';
        const formateador = new Intl.NumberFormat('es-VE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

        items.forEach(item => {
            const row = document.createElement('tr');
            row.classList.add('row-clickable');
            row.setAttribute('data-id', item.id);
            row.onclick = function () {
                window.location.href = `/Bienes/Edit/${item.id}`;
            };
            row.innerHTML = `
                <td>${item.grupo}</td>
                <td>${item.subgrupo || ''}</td>
                <td>${item.seccion || ''}</td>
                <td>${item.numeroIdentificacion}</td>
                <td>${item.marca}</td>
                <td>${item.nombre}</td>
                <td>${formateador.format(item.valorUnitario)}</td>
            `;
            tbody.appendChild(row);
        });

        const totalEl = document.getElementById('totalDisplay');
        if (totalEl) totalEl.innerText = totalRegistros.toLocaleString();
        const inicio = ((paginaActual - 1) * filasPorPagina) + 1;
        const fin = Math.min(paginaActual * filasPorPagina, totalRegistros);
        const rangoInicio = document.getElementById('rangoInicio');
        const rangoFin = document.getElementById('rangoFin');
        if (rangoInicio) rangoInicio.innerText = inicio;
        if (rangoFin) rangoFin.innerText = fin;

        const totalPaginas = Math.ceil(totalRegistros / filasPorPagina);
        renderizarBotones(totalPaginas, totalRegistros);

    } catch (error) {
        console.error('Error al actualizar la tabla:', error);
        tbody.innerHTML = '<tr><td colspan="7" class="text-center text-danger">Error al cargar datos</td></tr>';
    }
}

function renderizarBotones(total, totalRegistros) {
    const contenedor = document.getElementById('paginacionLista');
    if (!contenedor) return;
    contenedor.innerHTML = '';

    if (totalRegistros === 0) return;

    contenedor.innerHTML += `
        <li class="page-item ${paginaActual === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="event.preventDefault(); irAPagina(${paginaActual - 1})">&lt;</a>
        </li>`;

    for (let i = 1; i <= total; i++) {
        if (i === 1 || i === total || (i >= paginaActual - 1 && i <= paginaActual + 1)) {
            contenedor.innerHTML += `
                <li class="page-item ${i === paginaActual ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="event.preventDefault(); irAPagina(${i})">${i}</a>
                </li>`;
        } else if (i === paginaActual - 2 || i === paginaActual + 2) {
            contenedor.innerHTML += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
    }

    contenedor.innerHTML += `
        <li class="page-item ${paginaActual === total ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="event.preventDefault(); irAPagina(${paginaActual + 1})">&gt;</a>
        </li>`;
}

function irAPagina(p) {
    if (p < 1) return;
    paginaActual = p;
    actualizarTablaInventario(paginaActual, filasPorPagina);
}

function cambiarCantidad() {
    filasPorPagina = parseInt(document.getElementById('selectResultados').value);
    paginaActual = 1;
    actualizarTablaInventario(paginaActual, filasPorPagina);
}

document.addEventListener('DOMContentLoaded', () => {
    actualizarTablaInventario(1, filasPorPagina);

    document.getElementById('btnBuscar')?.addEventListener('click', () => {
        paginaActual = 1;
        actualizarTablaInventario(1, filasPorPagina);
    });

    document.getElementById('limpiarFiltros')?.addEventListener('click', () => {
        document.querySelectorAll('.search-filter-container input, .search-filter-container select').forEach(el => {
            el.value = '';
        });
        paginaActual = 1;
        actualizarTablaInventario(1, filasPorPagina);
    });
});
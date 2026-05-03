let paginaActual = 1;
let filasPorPagina = 10;

async function actualizarTablaInventario(pagina = 1, resultadosPorPagina = 10) {
    const tbody = document.getElementById('inventoryTable');

    const url = `/api/lista?pagina=${pagina}&resultadosPorPagina=${resultadosPorPagina}`;

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Error en la petición: ${response.statusText}`);
        }

        const datos = await response.json();

        tbody.innerHTML = '';

        const formateador = new Intl.NumberFormat('es-VE', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });

        datos.forEach(item => {
            const row = document.createElement('tr');

            const valorFormateado = formateador.format(item.valorUnitario);

            row.onclick = function () {
                window.location.href = `/Bienes/Edit/${item.id}`;
            };

            row.classList.add('row-clickable');

            row.innerHTML = `
                <td>${item.grupo}</td>
                <td>${item.numeroIdentificacion}</td>
                <td>${item.marca}</td>
                <td>${item.nombre}</td>
                <td>${valorFormateado}</td>
            `;

            tbody.appendChild(row);
        });

    } catch (error) {
        console.error('Error al actualizar la tabla:', error);
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">Error al cargar datos</td></tr>`;
    }
}

function inicializarPaginacion() {
    // 1. Obtener el total desde el dataset.id como pediste
    const elTotal = document.getElementById('totalBienes');
    const totalRegistros = parseInt(elTotal.dataset.id) || 0;

    // 2. Calcular total de páginas
    const totalPaginas = Math.ceil(totalRegistros / filasPorPagina);

    // 3. Actualizar textos informativos
    document.getElementById('totalDisplay').innerText = totalRegistros.toLocaleString();
    const inicio = ((paginaActual - 1) * filasPorPagina) + 1;
    const fin = Math.min(paginaActual * filasPorPagina, totalRegistros);

    document.getElementById('rangoInicio').innerText = inicio;
    document.getElementById('rangoFin').innerText = fin;

    // 4. Renderizar botones de página
    renderizarBotones(totalPaginas);
}

function renderizarBotones(total) {
    const contenedor = document.getElementById('paginacionLista');
    contenedor.innerHTML = '';

    // Botón Anterior
    contenedor.innerHTML += `
        <li class="page-item ${paginaActual === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="irAPagina(${paginaActual - 1})">&lt;</a>
        </li>`;

    // Generar números (Simplificado: 1, 2, 3...)
    for (let i = 1; i <= total; i++) {
        // Lógica para mostrar solo algunas páginas si hay demasiadas (opcional)
        if (i === 1 || i === total || (i >= paginaActual - 1 && i <= paginaActual + 1)) {
            contenedor.innerHTML += `
                <li class="page-item ${i === paginaActual ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="irAPagina(${i})">${i}</a>
                </li>`;
        } else if (i === paginaActual - 2 || i === paginaActual + 2) {
            contenedor.innerHTML += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
    }

    // Botón Siguiente
    contenedor.innerHTML += `
        <li class="page-item ${paginaActual === total ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="irAPagina(${paginaActual + 1})">&gt;</a>
        </li>`;
}

function irAPagina(p) {
    if (p < 1) return;
    paginaActual = p;
    actualizarTablaInventario(paginaActual, filasPorPagina);
    inicializarPaginacion();
}

function cambiarCantidad() {
    filasPorPagina = parseInt(document.getElementById('selectResultados').value);
    paginaActual = 1;
    actualizarTablaInventario(paginaActual, filasPorPagina);
    inicializarPaginacion();
}

// Ejecutar al cargar la página
document.addEventListener('DOMContentLoaded', inicializarPaginacion);

document.addEventListener('DOMContentLoaded', () => {
    actualizarTablaInventario(0, 10);

    //Swal.fire({
    //    title: 'Error!',
    //    text: 'Do you want to continue',
    //    icon: 'error',
    //    confirmButtonText: 'Cool',
    //    theme: 'auto'
    //})
});

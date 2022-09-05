let config;
let importFormat = 'json';
let manualCircle = false;
let onlyUseIni = false;
let circleSize = 70;
let viewOnlyMode = false;
const minCircleSize = 1;
const maxCircleSize = 1000;

const attribution = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';
const circleOptions = {
    color: 'red',
    fillColor: '#f03',
    fillOpacity: 0.5,
    draggable: true,
    radius: circleSize,
};
const shapeOptions = {
    stroke: true,
    color: '#3388ff',
    weight: 3,
    opacity: 1,
    fill: true,
    fillColor: null,
    fillOpacity: 0.2,
};

const drawnItems = new L.FeatureGroup();
const circleLayer = new L.FeatureGroup();
const mapOptions = {
    attribution,
    preferCanvas: true,
    updateWhenIdle: true,
    updateWhenZooming: false,
    layers: [drawnItems, circleLayer],
};
const map = L.map('map', mapOptions);

const initMap = (viewOnly, options) => {
    viewOnlyMode = viewOnly;
    config = options;

    const startLocation = [config.StartLatitude, config.StartLongitude];
    const startZoom = config.StartZoom;
    const minZoom = config.MinimumZoom;
    const maxZoom = config.MaximumZoom;
    const tileserverUrl = config.TileserverUrl;
    const scale = L.Browser.retina ? '@2x' : '';

    const tileserverOptions = {
        minZoom,
        maxZoom,
        attribution,
        scale,
        hq: L.Browser.retina,
    };
    L.tileLayer(tileserverUrl, tileserverOptions).addTo(map);
    map.setView(startLocation, startZoom);

    L.control.locate({
        icon: 'fa-solid fa-crosshairs',
        setView: 'untilPan',
        keepCurrentZoomLevel: true,
    }).addTo(map);

    let buttonManualCircle;
    if (!viewOnlyMode) {
        map.on('click', function (e) {
            if (!manualCircle) {
                return;
            }
            createCircle(e.latlng.lat, e.latlng.lng);
            setGeofence();
        });

        map.on('draw:created', (e) => {
            if (drawnItems) {
                const layer = e.layer;
                drawnItems.addLayer(layer);
                const layerName = manualCircle ? 'circleLayer' : 'drawnItems';
                const htmlDeleteButton = getDeleteButton(layer._leaflet_id, 'deleteLayer', layerName);
                layer.bindPopup(htmlDeleteButton);
            }
            setGeofence();
        });
        map.on('draw:edited', (e) => setGeofence());
        map.on('draw:deleted', (e) => setGeofence());
        map.on('draw:drawstart', (e) => {
            manualCircle = false;
            buttonManualCircle.state('enableManualCircle');
        });

        const drawControl = new L.Control.Draw({
            edit: {
                featureGroup: drawnItems,
            },
            draw: {
                polyline: false,
                polygon: {
                    allowIntersection: true,
                    showArea: true,
                    metric: 'km',
                    precision: {
                        km: 2,
                    },
                    shapeOptions,
                },
                rectangle: {
                    showRadius: true,
                    metric: true,
                    shapeOptions,
                },
                circle: false,
                marker: false,
                circlemarker: false,
            },
        });
        map.addControl(drawControl);
    }

    if (!viewOnlyMode) {
        const buttonImportGeofence = L.easyButton({
            states: [{
                icon: 'fa-solid fa-file-import',
                title: 'Import geofence',
                onClick: (button, map) => $('#importModal').modal('show'),
            }]
        });

        buttonManualCircle = L.easyButton({
            states: [{
                stateName: 'enableManualCircle',
                icon: 'far fa-circle',
                title: 'Enable manual circle mode',
                onClick: (button, map) => {
                    manualCircle = true;
                    button.state('disableManualCircle');
                },
            }, {
                stateName: 'disableManualCircle',
                icon: 'fas fa-circle',
                title: 'Disable manual circle mode',
                onClick: (button, map) => {
                    manualCircle = false;
                    button.state('enableManualCircle');
                },
            }]
        });

        const buttonGenerateRoute = L.easyButton({
            states: [{
                icon: 'fa-solid fa-shapes',
                title: 'Generate route',
                onClick: (button, map) => generateRoute(),
            }]
        });

        const buttonDeleteAllLayers = L.easyButton({
            states: [{
                icon: 'fa-solid fa-eraser',
                title: 'Delete all layers',
                onClick: (button, map) => {
                    if (window.confirm('Are you sure you want to delete all shapes?')) {
                        drawnItems.clearLayers();
                        circleLayer.clearLayers();
                    }
                },
            }]
        });

        const buttonSetCircleSize = L.easyButton({
            states: [{
                icon: 'fa-solid fa-pen-to-square',
                title: 'Set circle size',
                onClick: (button, map) => setCircleSize(),
            }]
        });

        const barRoute = L.easyBar([
            buttonImportGeofence,
            buttonManualCircle,
            buttonSetCircleSize,
            buttonDeleteAllLayers,
        ], { position: 'topleft' }).addTo(map);

        buttonGenerateRoute.addTo(map);
    }
};

const setGeofence = () => {
    const value = getGeofence();
    $('#Data_Area').val(value);
    $('#exportModal #Geofence').val(value);
};

const getGeofence = () => {
    const geofenceType = $('#Type').val();
    let value = '';
    if (geofenceType == 0) {
        // Circles
        let coords = '';
        const circles = turf.flip(circleLayer.toGeoJSON());
        for (const layer of circles.features) {
            const coord = layer.geometry.coordinates;
            coords += `${coord[0]},${coord[1]}\n`;
        }
        value = coords;
    } else {
        // Geofences
        const geofence = drawnItems.toGeoJSON();
        if (onlyUseIni) {
            for (const feature of geofence.features) {
                const iniData = geoJsonToIni(feature);
                value += iniData;
            }
        } else {
            value = JSON.stringify(geofence, null, 2);
        }
    }
    return value;
};

const getGeofenceData = () => {
    const name = $('#geofences').val();
    if (!name) {
        return;
    }
    $.ajax({
        url: '/GetGeofenceData',
        method: 'GET',
        data: { name },
    }).done(function (data) {
        const { type, geofence } = data;
        if (type === 0) {
            loadCircles(geofence);
        } else {
            loadGeofence(geofence, true);
        }
    });
}

const setReturnGeofenceType = (onlyIni) => onlyUseIni = onlyIni;

/*
map.on('click', '.deleteLayer', function (e) {
    console.log('delete layer click');
    const id = $(this).attr('data-layer-id');
    const container = $(this).attr('data-layer-container');
    deleteLayer(id, container);
});
*/

const deleteLayer = (id, container) => {
    //console.log('id:', id, 'container:', container);
    switch (container) {
        case 'circleLayer':
            circleLayer.removeLayer(id);
            break;
        case 'drawnItems':
            drawnItems.removeLayer(id);
            break;
    }
};

const getDeleteButton = (id, className = 'deleteLayer', layerName = 'circleLayer', text = 'Delete') => {
    //const html = `<button class="btn btn-secondary btn-sm ${className}" data-layer-container="${layerName}" data-layer-id=${id} type="button">${text}</button></div>`;
    const html = `<button class="btn btn-secondary btn-sm ${className}" type="button" onclick="deleteLayer(${id}, '${layerName}');">${text}</button></div>`;
    return html;
};

const importFormatChanged = (e) => importFormat = e.value;

const importGeofence = () => {
    const geofenceData = $('#importModal #Geofence').val();
    if (!geofenceData) {
        return;
    }
    const importType = $('#importModal #Type').val();
    if (!importType) {
        // TODO: Set error in modal
        return;
    }
    const geofence = formatGeofenceToGeoJson(importFormat, geofenceData);
    loadGeofence(geofence);
    $('#importModal').modal('hide');
};

const exportFormatChanged = (e) => {
    const exportFormat = e.value;
    const geofence = $('#exportModal #Geofence').val();

    switch (exportFormat) {
        case 'json':
            // Convert ini to json geofence
            const geojson = iniToGeoJson(geofence);
            const json = JSON.stringify(geojson, null, 2);
            //console.log('json:', json);
            $('#exportModal #Geofence').val(json);
            break;
        case 'txt':
        case 'ini':
            // Convert json to ini geofence
            const iniData = [];
            drawnItems.eachLayer(layer => {
                const geojson = layer.toGeoJSON();
                const data = geoJsonToIni(geojson);
                iniData.push(data);
            });
            const ini = iniData.join('');
            //console.log('ini:', ini);
            $('#exportModal #Geofence').val(ini);
            break;
    }
};

const exportGeofence = () => {
    const geofence = $('#exportModal #Geofence').val();
    if (geofence) {
        copyToClipboard(geofence);
    }
};

const formatGeofenceToGeoJson = (format, data) => {
    //console.log('format:', format, 'data:', data);
    if (data.length === 0) {
        return null;
    }
    if (typeof data === 'object') {
        return data;
    }
    switch (format) {
        case 'json':
            return JSON.parse(data);
        case 'txt':
        case 'ini':
            return iniToGeoJson(data);
        default:
            throw Error('Unsupported geofence format');
    }
};

const loadGeofence = (data, convertToJson) => {
    if (!drawnItems) {
        return;
    }
    if (convertToJson) {
        data = iniToGeoJson(data);
    }
    const leafletGeoJSON = new L.GeoJSON(data);
    leafletGeoJSON.eachLayer(layer => {
        const areaSizeKm = getAreaSize(layer);
        const html = `
            <b>Name:</b> ${layer.feature.properties.name}<br>
            <b>Area:</b> ${areaSizeKm} km²
`;
        layer.bindTooltip(html);
        drawnItems.addLayer(layer);
    });

    if (!viewOnlyMode) {
        setGeofence();
    }

    // Set map center to first coord
    const features = data.features;
    if (features.length > 0) {
        const feature = features[0];
        const coord = feature.geometry.coordinates[0][1];
        if (coord) {
            map.setView([coord[1], coord[0]]);
        }
    }
};

const loadCircles = (data) => {
    const circles = data.split('\n');
    // Set map center to first coord
    const split = circles[1].split(',');
    if (split.length === 2) {
        map.setView([split[0], split[1]]);
    }
    for (const circle of circles) {
        const split = circle.split(',');
        if (split.length === 2) {
            createCircle(split[0], split[1]);
        }
    }

    if (!viewOnlyMode) {
        setGeofence();
    }
};

const createCircle = (lat, lng) => {
    circleOptions.radius = circleSize;
    L.circle([lat, lng], circleOptions).bindPopup((layer) => {
        return getDeleteButton(layer._leaflet_id);
    }).addTo(circleLayer);
};

const generateRoute = () => {
    circleLayer.clearLayers();

    const xMod = Math.sqrt(0.75);
    const yMod = Math.sqrt(0.568);
    const nextCircle = circleSize * 1.5;
    const route = (layer) => {
        const poly = layer.toGeoJSON();
        const line = turf.polygonToLine(poly);
        const bounds = layer.getBounds();

        let currentLatLng = bounds.getNorthEast();
        const startLatLng = L.GeometryUtil.destination(currentLatLng, 90, nextCircle);
        const endLatLng = L.GeometryUtil.destination(L.GeometryUtil.destination(bounds.getSouthWest(), 270, nextCircle), 180, circleSize);

        let row = 0;
        let heading = 270;
        while (currentLatLng.lat > endLatLng.lat) {
            do {
                const point = turf.point([currentLatLng.lng, currentLatLng.lat]);
                const distance = turf.pointToLineDistance(point, line, { units: 'meters' });
                if (distance <= circleSize || distance == 0 || turf.inside(point, poly)) {
                    newCircle = createCircle(currentLatLng.lat, currentLatLng.lng);
                }
                currentLatLng = L.GeometryUtil.destination(currentLatLng, heading, (xMod * circleSize * 2));
            } while ((heading == 270 && currentLatLng.lng > endLatLng.lng) || (heading == 90 && currentLatLng.lng < startLatLng.lng));

            currentLatLng = L.GeometryUtil.destination(currentLatLng, 180, (yMod * circleSize * 2));

            rem = row % 2;
            heading = rem === 1 ? 270 : 90;
            currentLatLng = L.GeometryUtil.destination(currentLatLng, heading, (xMod * circleSize) * 3);
            row++;
        }
    };

    drawnItems.eachLayer((layer) => {
        route(layer);
    });
}

const getAreaSize = (layer, decimals = 2) => {
    // Get area size of geofence
    const latlng = layer.getLatLngs()[0];
    const area = L.GeometryUtil.geodesicArea(latlng);
    const readableArea = L.GeometryUtil.formattedNumber(area * 0.000001, decimals);
    return readableArea;
};

const setCircleSize = () => {
    const result = prompt('Enter the circle size to use when generating a route:', circleSize);
    if (!result) {
        // Pressed cancel
        return;
    }
    const value = parseInt(result);
    if (value >= minCircleSize && value <= maxCircleSize) {
        circleSize = value;
    }
}
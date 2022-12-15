const iniToGeoJson = (data) => {
    if (!data) {
        return undefined;
    }
    const geoJson = {
        type: 'FeatureCollection',
        features: [],
    };
    const fences = data.match(/\[([^\]]+)\]([^[]*)/g);
    if (!fences) {
        return undefined;
    }
    for (const fence of fences) {
        const geofence = {
            type: 'Feature',
            properties: {
                name: fence.match(/\[([^\]]+)\]/)[1],
            },
            geometry: {
                type: 'Polygon',
                coordinates: [[]],
            },
        };
        const coordinates = fence.match(/[0-9\-.]+,\s*[0-9\-.]+/g).map((point) => [parseFloat(point.split(',')[1]), parseFloat(point.split(',')[0])]);
        const first = coordinates[0];
        const last = coordinates[coordinates.length - 1];
        // Ensure first coordinate is also the last coordinate
        if (first !== last) {
            coordinates.push(first);
        }
        geofence.geometry.coordinates[0] = coordinates;
        geoJson.features.push(geofence);
    }
    return geoJson;
};

const geoJsonToIni = (feature) => {
    if (!feature) {
        return undefined;
    }
    let geofence = [];
    if (feature.geometry.type === 'Polygon') {
        geofence.push(`[${feature.properties.name}]\n`);
        for (const coord of feature.geometry.coordinates) {
            coord.pop();
            for (const point of coord) {
                geofence.push(`${point[1]},${point[0]}\n`);
            }
        }
    }
    return geofence.join('');
};
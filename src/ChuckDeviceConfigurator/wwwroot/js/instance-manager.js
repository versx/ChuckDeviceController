function hideElements(className, show) {
    const elements = document.getElementsByClassName(className);
    for (const element of elements) {
        element.style.display = show ? 'block' : 'none';
    }
}

function parseGeofencesJson(json) {
    try {
        if (!json) {
            return null;
        }
        const geofences = JSON.parse(json);
        return geofences;
    } catch (e) {
        console.error('Failed to parse geofences list:', e);
        return null;
    }
}
function populateImage (row, type, set, meta) {
	console.log("Row:", row);
    const input = row.image;
	console.log("Input:", input);
    switch (input.type) {
        case 'img':
            return `<img class="lazy_load" src="/static/img/${input.path}"/>`;
        case 'device':
            return `<img class="lazy_load" src="/static/img/devices/${input.status}.png"/>`;
        default:
            return input;
    }
}
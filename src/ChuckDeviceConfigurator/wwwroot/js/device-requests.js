const successAlert = `
<div class="alert alert-dismissible alert-success d-flex align-items-center fade show" role="alert">
    <div>
        <svg class="bi flex-shrink-0 me-2" width="24" height="24" role="img">
            <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
        </svg>
        [PLACE_HOLDER]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
</div>
`;

const warningAlert = `
<div class="alert alert-dismissible alert-warning d-flex align-items-center fade show" role="alert">
    <div>
        <svg class="bi flex-shrink-0 me-2" width="24" height="24" role="img">
            <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z" />
        </svg>
        [PLACE_HOLDER]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
</div>
`;

const errorAlert = `
<div class="alert alert-dismissible alert-danger d-flex align-items-center fade show" role="alert">
    <div>
        <svg class="bi flex-shrink-0 me-2" width="24" height="24" role="img"">
            <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z" />
        </svg>
        [PLACE_HOLDER]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
</div>
`;

const alertAutoDismissIntervalMs = 5000;
const requestTimeoutMs = 3000;

// TODO: Make alerts configurable
// TODO: Make device endpoint configurable

const restartGame = (ipAddr) => {
    const statusAlertEl = document.getElementById('device-status');
    if (!ipAddr) {
        const alertMsg = warningAlert.replace('[PLACE_HOLDER]', '<strong>Warning!</strong> IP address for device is not set, skipping restart game request...');
        statusAlertEl.innerHTML += alertMsg;
        alertTimeout(alertAutoDismissIntervalMs);
        return;
    }

    const url = `http://${ipAddr}:8080/restart`;
    const options = { signal: AbortSignal.timeout(requestTimeoutMs) };
    fetch(url, options)
        .then(response => {
            //console.log('response:', response);
            const alertMsg = response.status === 200
                ? successAlert.replace('[PLACE_HOLDER]', `<strong>Success!</strong> Device at IP address ${ipAddr} has been restarted!`)
                : errorAlert.replace('[PLACE_HOLDER]', `<strong>Error!</strong> ${response.statusText}`);
            statusAlertEl.innerHTML += alertMsg;
        })
        .catch(err => {
            console.error('restartGame:', err);
            const alertMsg = errorAlert.replace('[PLACE_HOLDER]', `<strong>Error!</strong> ${err}`);
            statusAlertEl.innerHTML += alertMsg;
        });
    alertTimeout(alertAutoDismissIntervalMs);
};

const getScreenshot = (ipAddr) => {
    const statusAlertEl = document.getElementById('device-status');
    if (!ipAddr) {
        const alertMsg = warningAlert.replace('[PLACE_HOLDER]', '<strong>Warning!</strong> IP address for device is not set, skipping get screenshot request...');
        statusAlertEl.innerHTML += alertMsg;
        alertTimeout(alertAutoDismissIntervalMs);
        return;
    }

    const url = `http://${ipAddr}:8080/screen`;
    fetch(url)
        .then(response => response.blob())
        .then(response => {
            //console.log('response:', response);
            if (response) {
                const imageObjectUrl = URL.createObjectURL(response);
                const imgEl = document.getElementById('screenshot');
                imgEl.src = imageObjectUrl;
                if (imgEl.classList.contains('d-none')) {
                    imgEl.classList.remove('d-none');
                }
            } else {
                statusAlertEl.innerHTML += errorAlert.replace('[PLACE_HOLDER]', `<strong>Error!</strong> ${response.statusText}`);
            }
        })
        .catch(err => {
            console.error('getScreenshot:', err);
            const alertMsg = errorAlert.replace('[PLACE_HOLDER]', `<strong>Error!</strong> ${err}`);
            statusAlertEl.innerHTML += alertMsg;
        });
    alertTimeout(alertAutoDismissIntervalMs);
};

const alertTimeout = (wait) => {
    setTimeout(function () {
        $('#device-status').children('.alert-dismissible:first-child').remove();
    }, wait);
};
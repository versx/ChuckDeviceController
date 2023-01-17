const modalList = document.getElementById('modal-priority-list');
const sortableModal = new Sortable(modalList, {
    animation: 150,
    scroll: true,
    forceAutoscrollFallback: false,
    scrollSensitivity: 30,
    scrollSpeed: 10,
    bubleScroll: true,
    ghostClass: 'blue-background-class',
    onEnd: (evt) => setSelectedPokemon(),
});

function saveGeneratedList() {
    const children = [...modalList.children];
    const ids = children.map(x => x.getAttribute('data-id'));
    //console.log('ids:', ids);
    
    if ($('#pokemon-priority-list').children().length) {
        const confirmationMessage = 'There is already a priority list generated, generating a new one will overwrite the existing. Are you sure you want to do this?';
        if (!confirm(confirmationMessage)) {
            return;
        }
    }
    selectAllPokemon(false);
    for (const id of ids) {
        const item = document.querySelector(`#pokemon-list div.item[id='${id}']`);
        if (item) {
            selectItem(item);
        }
    }
}

function generateList() {
    // Send ignore list with request
    const pokemonIgnored = $('#pokemon-ignored').val() || '';
    const ignoreList = pokemonIgnored.includes('\n')
        ? pokemonIgnored.split('\n').join(',')
        : pokemonIgnored.split(',').join(',');
    fetch('/IvList/GeneratePokemonPriorityList?ignored=' + ignoreList) // &max_seen=10&limit=100
        .then(resp => resp.json())
        .then(resp => {
            //console.log('resp:', resp);
            clearList();
            const pokemonIds = resp || [];
            for (const id of pokemonIds) {
                const item = document.querySelector(`#pokemon-list div.item[id='${id}']`);
                if (item) {
                    addPriorityList(item, '#modal-priority-list');
                }
            }
        })
        .catch(err => {
            console.error('Error:', err);
        });
}

function clearList() {
    const children = [...modalList.children];
    const ids = children.map(x => x.getAttribute('data-id'));
    for (const id of ids) {
        removePriorityList(id, true, '#modal-priority-list');
    }
}
const PokemonGenerations = {
    gen1: { start: 1, end: 151 },
    gen2: { start: 152, end: 251 },
    gen3: { start: 252, end: 386 },
    gen4: { start: 387, end: 494 },
    gen5: { start: 495, end: 649 },
    gen6: { start: 650, end: 721 },
    gen7: { start: 722, end: 809 },
    gen8: { start: 810, end: 905 },
};

const listGroup = document.getElementById('pokemon-priority-list');
const sortable = new Sortable(listGroup, {
    animation: 150,
    scroll: true,
    forceAutoscrollFallback: false,
    scrollSensitivity: 30,
    scrollSpeed: 10,
    bubleScroll: true,
    ghostClass: 'blue-background-class',
    onEnd: (evt) => setSelectedPokemon(),
});


let pokemonRarity = {};
$.getJSON('/data/rarity.json', function(data) {
    pokemonRarity = data;
});

function selectByRarity(rarity) {
    $.each($('.item'), function (index, item) {
        const id = item.id.split('_f')[0];
        if (pokemonRarity[rarity].includes(parseInt(id))) {
            selectItem(item);
        }
    });
}

function selectAllPokemon(select) {
    const pokemon = getPokemonItems();
    const selectedPokemon = getSelectedPokemon().split(',');
    for (const pkmn of pokemon) {
        const isSelected = selectedPokemon.includes(pkmn.id);
        if (select && !isSelected) {
            selectItem(pkmn);
        } else {
            unselectItem(pkmn);
        }
    }
    
    if (!select) {
        setSelectedPokemon();
    }
}

function selectByGen(genNum) {
    const pokemon = getPokemonItems();
    const generation = PokemonGenerations['gen' + genNum];
    for (const pkmn of pokemon) {
        const id = pkmn.id.split('_f')[0];
        if (id >= generation.start && id <= generation.end) {
            selectItem(pkmn);
        }
    }
}

function invertSelection() {
    const value = getSelectedPokemon();
    const oldPokemon = value.split(',');
    const pokemon = getPokemonItems();
    for (const pkmn of pokemon) {
        //const isSelected = pkmn.classList.value.includes('active');
        const isSelected = pkmn.classList.contains('active');
        if (!isSelected && !oldPokemon.includes(pkmn.id)) {
            selectItem(pkmn);
        } else {
            unselectItem(pkmn);
        }
    }
}

function onPokemonClicked(element) {
    if (element.disabled) {
        return;
    }
    if (element.classList.value.includes('active')) {
        unselectItem(element);
    } else {
        selectItem(element);
    }
}

function onPokemonSearch() {
    const pokemon = $('#pokemon-list').children();
    const search = $('#search').val().toLowerCase();
    for (let element of pokemon) {
        const id = element.getAttribute('id');
        const name = element.getAttribute('data-name');
        let matches = false;
        if (search.includes(' ')) {
            matches = !search.includes(name.toLowerCase()) && !search.includes(id) && search;
        } else {
            matches = !name.toLowerCase().includes(search) && !id.includes(search) && search;
        }
        element.hidden = matches;
    }
}

function toggleAllPokemon(disable) {
    const pokemon = $('#pokemon-list').children();
    for (let element of pokemon) {
        element.disabled = disable;
    }
}

function selectItem(element) {
    const selectedPokemon = getSelectedPokemon().split(',');
    const id = element.id;
    if (selectedPokemon.includes(id)) {
        return;
    }
    addPriorityList(element);
    element.classList.toggle('active');
    element.classList.toggle('pokemon-selected');
    appendId(id);
    
    setPriorityCount();
}

function unselectItem(element) {
    const selectedPokemon = getSelectedPokemon().split(',');
    const id = element.id;
    if (!selectedPokemon.includes(id)) {
        return;
    }
    removePriorityList(id);
    element.classList.toggle('active');
    element.classList.toggle('pokemon-selected');
    removeId(id);
    
    setPriorityCount();
}

function appendId(id) {
    const value = getSelectedPokemon();
    let newValue = value;
    if (!value || value === '') {
        newValue = id;
    } else {
        if (value.endsWith(',')) {
            newValue = value + id;
        } else {
            newValue = value + ',' + id;
        }
    }
    setSelectedPokemon(newValue);
}

function removeId(id) {
    const value = getSelectedPokemon();
    const list = value.split(',')
    const newList = list.filter(x => x !== id);
    const newValue = newList.join(',');
    setSelectedPokemon(newValue);
}

function getSelectedPokemon() {
    const pokemonIds = getPokemonIdsElement();
    const value = pokemonIds.value;
    return value || '';
}

function setSelectedPokemon(pokemon) {
    const pokemonIds = getPokemonIdsElement();
    if (pokemon) {
        pokemonIds.value = pokemon;
    } else {
        const sorted = sortable.toArray();
        if (sorted) {
            pokemonIds.value = sorted.join(',');
        }
    }
    
    setPriorityCount();
}

function addPriorityList(element) {
    const id = element.id;
    const name = element.getAttribute('data-name');
    const image = element.getAttribute('data-image');
    const pokemonId = element.getAttribute('data-pokemon-id');
    $('#pokemon-priority-list').append(`
<li data-id="${id}" class="list-group-item">
    <div class="row">
        <div class="col col-md-2 col-sm-2 px-1">
            <img src="${image}" width="32" height="32" />
        </div>
        <div class="col col-md-10 col-sm-10">
            <small class="caption">${name} <small>(#${pokemonId})</small></small>
        </div>
    </div>
</li>`);
}

function removePriorityList(id) {
    $(`#pokemon-priority-list [data-id='${id}']`).remove();
}

function getPokemonIdsElement() {
    const pokemonIds = document.getElementById('PokemonIds');
    return pokemonIds;
}

function getPokemonItems() {
    const pokemon = document.getElementsByClassName('item');
    return pokemon;
}

function setPriorityCount() {
    const count = document.getElementById('pokemon-priority-list').children.length;
    document.getElementById('priority-count').innerText = `(${count})`;
}
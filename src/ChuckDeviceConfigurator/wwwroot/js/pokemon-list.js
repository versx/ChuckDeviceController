const PokemonGenerations = {
    gen1: { start: 1, end: 151 },
    gen2: { start: 152, end: 251 },
    gen3: { start: 252, end: 386 },
    gen4: { start: 387, end: 494 },
    gen5: { start: 495, end: 649 },
    gen6: { start: 650, end: 721 },
    gen7: { start: 722, end: 809 },
    gen8: { start: 810, end: 898 },
};

const listGroup = document.getElementById('pokemon-priority-list');
const sortable = new Sortable(listGroup, {
    animation: 150,
    ghostClass: 'blue-background-class',
    onEnd: function (evt) {
        setSelectedPokemon();
    },
});


/*
let pokemonRarity = {};
$.getJSON('/data/rarity.json', function(data) {
    pokemonRarity = data;
});
*/

function initButtons() {
    $('#select_rare').on('click', function () {
        $.each($('.item'), function (index, item) {
            if (!pokemonRarity.common.includes(item.id)) {
                selectItem(item);
            }
        });
    });
    $('#select_ultra').on('click', function () {
        $.each($('.item'), function (index, item) {
            if (pokemonRarity.ultra.includes(item.id)) {
                selectItem(item);
            }
        });
    });
    $('#select_raid5star').on('click', function () {
        $.each($('.item'), function (index, item) {
            if (pokemonRarity.raid5star.includes(item.id)) {
                selectItem(item);
            }
        });
    });
    $('#select_raid6star').on('click', function () {
        $.each($('.item'), function (index, item) {
            if ((pokemonRarity.raid6star || []).includes(item.id)) {
                selectItem(item);
            }
        });
    });
}

function selectAllPokemon(select) {
    const pokemon = document.getElementsByClassName('item');
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
        setSelectedPokemon('');
    }
}

function selectByGen(genNum) {
    const pokemon = document.getElementsByClassName('item');
    const generation = PokemonGenerations['gen' + genNum];
    for (const pkmn of pokemon) {
        if (pkmn.id >= generation.start && pkmn.id <= generation.end) {
            selectItem(pkmn);
        }
    }
}

function invertSelection() {
    const value = getSelectedPokemon() || '';
    const oldPokemon = value.split(',');
    const pokemon = document.getElementsByClassName('item');
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
        const name = element.getAttribute('name');
        const id = element.getAttribute('id');
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
    const name = element.getAttribute('data-name');
    const image = element.getAttribute('data-image');
    $('#pokemon-priority-list').append(`
<li data-id="${id}" class="list-group-item">
    <div class="row">
        <div class="col col-md-2 col-sm-2">
            <img src="${image}" width="48" height="48" />
        </div>
        <div class="col col-md-10 col-sm-10">
            <small class="caption">${name} <small>(#${id})</small></small>
        </div>
    </div>
</li>`);
    element.classList.toggle('active');
    element.style.background = 'dodgerblue';
    appendId(id);
}

function unselectItem(element) {
    const selectedPokemon = getSelectedPokemon().split(',');
    const id = element.id;
    if (!selectedPokemon.includes(id)) {
        return;
    }
    $(`#pokemon-priority-list [data-id='${id}']`).remove();
    element.classList.toggle('active');
    element.style.background = $('.pokemon-list').css('background-color');
    removeId(id);
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
    const list = (value || '').split(',')
    const newList = list.filter(x => x !== id);
    const newValue = newList.join(',');
    setSelectedPokemon(newValue);
}

function getSelectedPokemon() {
    const pokemonIds = document.getElementById('PokemonIds');
    const value = pokemonIds.value;
    return value || '';
}

function setSelectedPokemon(pokemon) {
    const pokemonIds = document.getElementById('PokemonIds');
    if (pokemon) {
        pokemonIds.value = pokemon;
    } else {
        const sorted = sortable.toArray();
        if (sorted) {
            pokemonIds.value = sorted.join(',');
        }
    }
}
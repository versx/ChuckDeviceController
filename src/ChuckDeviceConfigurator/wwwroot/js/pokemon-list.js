let pokemonRarity = {};

/*
$('form').submit(function(e) {
    const pokemon = $('#PokemonIds').val();
    if (pokemon == '') {
        // Show error message about pokemon selection
        $('#error-div').prop('hidden', false);
        $('#error-div').html('<div><strong>Error!</strong> Please select one or more pokemon.</div>');
        e.preventDefault();
    }
});

$.getJSON('/data/rarity.json', function(data) {
    pokemonRarity = data;
});
*/

function initButtons() {
    document.body.addEventListener('click', '.pokemon-button', function (e) {
        console.log('button clicked');
    });
    $('#select_all').on('click', function() {
        $.each($('.item'), function(index, item) {
            selectItem(item);
        });
    });
    $('#select_none').on('click', function() {
        $.each($('.item'), function(index, item) {
            unselectItem(item);
            $('#PokemonIds').val('');
        });
    });
    $('#select_gen1').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id < 152) {
                selectItem(item);
            }
        });
    });
    $('#select_gen2').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id > 151 && item.id < 252) {
                selectItem(item);
            }
        });
    });
    $('#select_gen3').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id > 251 && item.id < 387) {
                selectItem(item);
            }
        });
    });
    $('#select_gen4').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id > 386 && item.id < 495) {
                selectItem(item);
            }
        });
    });
    $('#select_gen5').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id > 494 && item.id < 650) {
                selectItem(item);
            }
        });
    });
    $('#select_gen6').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id > 649 && item.id < 722) {
                selectItem(item);
            }
        });
    });
    $('#select_gen7').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id > 721 && item.id < 810) {
                selectItem(item);
            }
        });
    });
    $('#select_gen8').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (item.id > 809 && item.id < 899) {
                selectItem(item);
            }
        });
    });
    $('#select_rare').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (!pokemonRarity.common.includes(parseInt(item.id))) {
                selectItem(item);
            }
        });
    });
    $('#select_ultra').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (pokemonRarity.ultra.includes(parseInt(item.id))) {
                selectItem(item);
            }
        });
    });
    $('#select_raid5star').on('click', function() {
        $.each($('.item'), function(index, item) {
            if (pokemonRarity.raid5star.includes(parseInt(item.id))) {
                selectItem(item);
            }
        });
    });
    $('#select_raid6star').on('click', function() {
        $.each($('.item'), function(index, item) {
            if ((pokemonRarity.raid6star || []).includes(parseInt(item.id))) {
                selectItem(item);
            }
        });
    });
    $('#select_invert').on('click', function() {
        const value = $('#PokemonIds').val() || '';
        const oldPokemon = value.split(',')
                                .map(Number);
        $.each($('.item'), function(index, item) {
            const isPokemonSelected = item.classList.value.includes('active');
            if (!isPokemonSelected && !oldPokemon.includes(item.id)) {
                selectItem(item);
            } else {
                unselectItem(item);
            }
        });
    });
}

function onPokemonClicked(element) {
    if (element.disabled)
        return;
    if (element.classList.value.includes('active')) {
        element.classList.value = element.classList.value.replace(' active', '');
        element.style.background = $('.pokemon-list').css('background-color');
        removeId(element.id);
    } else {
        element.classList.value = element.classList.value += ' active';
        element.style.background = 'dodgerblue';
        appendId(element.id);
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
    if (!element.classList.value.includes('active')) {
        element.classList.value = element.classList.value += ' active';
    }
    element.style.background = 'dodgerblue';
    appendId(element.id);
}

function unselectItem(element) {
    if (element.classList.value.includes('active')) {
        element.classList.value = element.classList.value.replace(' active', '');
    }
    element.style.background = $('.pokemon-list').css('background-color');
    removeId(element.id);
}

function appendId(id) {
    const value = $('#PokemonIds').val();
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
    $('#PokemonIds').val(newValue);
}

function removeId(id) {
    const value = $('#PokemonIds').val();
    const list = (value || '').split(',').map(Number);
    const newList = list.filter(x => x !== parseInt(id));
    const newValue = newList.join(',');
    $('#PokemonIds').val(newValue);
}

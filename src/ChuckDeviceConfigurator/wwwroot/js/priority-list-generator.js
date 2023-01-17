function generateIvList() {
    if ($('#pokemon-priority-list').children().length) {
        const confirmationMessage = 'There is already a priority list generated, generating a new one will clear the existing. Are you sure you want to do this?';
        if (!confirm(confirmationMessage)) {
            return;
        }
    }
    fetch('/IvList/GeneratePokemonPriorityList')
        .then(resp => resp.json())
        .then(resp => {
            console.log('resp:', resp);
            const pokemonIds = resp || [];
            selectAllPokemon(false);
            for (const id of pokemonIds) {
                const item = document.querySelector(`#pokemon-list div.item[id='${id}']`);
                if (item) {
                    selectItem(item);
                } else {
                    console.warn('id:', id, 'does not exist');
                }
            }
        })
        .catch(err => {
            console.error('Error:', err);
        });
}
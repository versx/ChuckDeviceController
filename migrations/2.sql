ALTER TABLE `instance`
MODIFY COLUMN `type` enum('circle_pokemon','circle_raid','circle_smart_raid','auto_quest','pokemon_iv','bootstrap','find_tth') NOT NULL;
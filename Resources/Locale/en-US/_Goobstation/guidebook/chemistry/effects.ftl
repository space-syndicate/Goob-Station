reagent-effect-guidebook-deal-stamina-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Deals
                *[-1] Heals
            }
        *[other]
            { $deltasign ->
                [1] deal
                *[-1] heal
            }
    } { $amount } { $immediate ->
                    [true] immediate
                    *[false] overtime
                  } stamina damage

reagent-effect-guidebook-stealth-entities = Camouflages living mobs nearby.

reagent-effect-guidebook-change-faction = Changes the mob's faction to {$faction}.

reagent-effect-guidebook-mutate-plants-nearby = Randomly mutates nearby plants.

reagent-effect-guidebook-dnascramble = Scrambles the person's DNA.

reagent-effect-guidebook-change-species = Turns the target into a {$species}.

reagent-effect-guidebook-change-species-random = Turns the target into a completely random species.

reagent-effect-guidebook-sex-change = Swaps the person's gender


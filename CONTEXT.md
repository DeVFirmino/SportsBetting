# Sports Betting Study

An educational sports-betting context for practising how an API owns identity,
pricing and simulated balance changes. It has no real-money users and does not
settle sporting outcomes.

## Language

**Fixture**:
A scheduled sporting event on which a bet may be placed. Its teams and schedule
come from the football-data provider, but its price does not.
_Avoid_: Match, game

**User**:
The person authenticated by the study API who owns one wallet and their bets.
_Avoid_: Bettor, player, account

**Bet**:
A user's selected outcome for a fixture together with a stake and the odds
assigned when it is placed. The current scope records placement, not the
outcome of the fixture.
_Avoid_: Wager, ticket

**Betting market**:
The outcome selected for a bet: home win, draw or away win.
_Avoid_: Bet type

**Stake**:
The simulated amount committed from a wallet when a bet is placed.
_Avoid_: Bet amount, price

**Odds**:
A fixed study value assigned by the service to a betting market. It is not a
live bookmaker price and is never supplied by the user.
_Avoid_: Rate, multiplier

**Potential return**:
The estimated stake multiplied by the assigned odds. It is not a payout and is
never credited by the current system.
_Avoid_: Winnings, prize, payout

**Wallet**:
The user's simulated balance from which stakes are deducted. It does not hold
or represent real money.
_Avoid_: Account, bank account

**Deposit**:
An addition of simulated funds to a wallet for exercising the betting flow. It
does not represent a payment or transfer from an external source.
_Avoid_: Payment, top-up transaction

**Settlement**:
The determination of whether a bet won or lost and any resulting wallet credit.
Settlement is outside the current system's scope.
_Avoid_: Resolution, payout

CryptoNote-Easy-Miner
===

This is a simple C# app that helps Windows users start mining without dealing with command-line operated binaries. It is bundled with the latest 32 & 64 bit builds of simpleminer and simplewallet.


Upon starting for the first time it will run simplewallet to generate a new address (with a default wallet password of `x`). The user can then input a pool host & port, select how many CPU cores they want to use, the click `Start Mining`.


The app will spawn instances of simpleminer for each core with the approperiate command-line arguments.


####Download

Get the latest build here: [releases](//github.com/zone117x/cryptonote-easy-miner/releases)

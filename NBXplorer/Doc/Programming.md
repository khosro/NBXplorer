#### git clone "https://github.com/khosro/cryptocurrency-daemon-utils" and follow ```Instruction.md```

Run ```litecoinStart.bat``` and ```bitcoinStart.bat```

####  NBXplorer

```
git clone --recursive  <NBXplorer_Project>
for b in `git branch -r | grep -v -- '->'`; do git branch --track ${b##origin/} $b; done
git checkout <MainBranch_ASK_From_Admin>
git pull --recurse-submodules
```

You must restart VS to applied changes in VS when to run app.

In VS select ```Profile_ENV``` profile.The run the following command.

```<Btcpay_Data_Home>``` is the same value of ```Btcpay_Data_Home``` in ```Instruction.md```(cryptocurrency-daemon-utils)

```
SETX NBXPLORER_CONF "<Btcpay_Data_Home>\NBXplorer\NBXplorer.config"
SETX NBXPLORER_CHAINS "btc,ltc"  // We can also set it in NBXplorer.config file by parameter "chains"
 ```

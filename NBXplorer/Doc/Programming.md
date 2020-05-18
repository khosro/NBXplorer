1. git clone "https://github.com/khosro/cryptocurrency-daemon-utils" and follow ```Instruction.md```

Run ```litecoinStart.bat``` and ```bitcoinStart.bat```

2. NBXplorer

```git clone <NBXplorer_Project>``` and then ```git checkout <MainBranch_ASK_From_Admin> ```

In VS select ```Profile_ENV``` profile.The run the following command.

```<Btcpay_Data_Home>``` is the same value of ```Btcpay_Data_Home``` in ```Instruction.md```(cryptocurrency-daemon-utils)

SETX NBXPLORER_CONF "<Btcpay_Data_Home>\NBXplorer\NBXplorer.config"
SETX NBXPLORER_CHAINS "btc,ltc"
 

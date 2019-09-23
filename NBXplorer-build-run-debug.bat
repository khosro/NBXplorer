dotnet build  .\NBXplorer\NBXplorer.csproj 
dotnet run -p .\NBXplorer\NBXplorer.csproj --conf="%Btcpay_Data_Home%\NBXplorer\NBXplorer.config"   -regtest --chains "btc" --datadir "%Btcpay_Data_Home%\NBXplorer\data" %*

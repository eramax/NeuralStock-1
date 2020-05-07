# Neural Stock
A desktop application to predict stock price movements using artificial neural networks. 

The program can predict price movements for stocks listed any exchange (provided the stock symbol can be found in Yahoo Finance), but the trasaction fees and taxes are only being considered for the PSI20 (Portugal) and STI (Singapore). The application downloads historical prices and trains the backpropagation neural network using a portion of the data; the network that performs the best in the testing sample -measured by the highest profit- is saved. A Monte Carlo approach is used to sample the strategy parameter's-space.

![alt text](http://i.imgur.com/XjoGzsA.png "NeuralStock")
![alt text](http://i.imgur.com/aMj8XO6.png "NeuralStock")

You can download the latest version (v1.1) [here](https://github.com/cesarioalmeida/NeuralStock/releases/download/v1.1/NeuralStock.exe).

# Neural Stock
A program to predict stock price movements using artificial neural networks. 

At the moment, the program can train/predict price movements for stocks listed on the PSI20 (Portugal) and STI (Singapore), but can easily be expanded to other markets if needed. The program automatically downloads historical prices, trains the backpropagation neural network using a portion of the data; the network that performs the best in the testing sample -measured by the highest profit- is saved. A Monte Carlo approach is used to sample the strategy parameter's-space.

![alt text](http://i.imgur.com/NlRF2ab.png "NeuralStock")
![alt text](http://i.imgur.com/aMj8XO6.png "NeuralStock")

//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.State;

namespace Nethermind.Blockchain.Processing
{
    public class TransactionProcessingStrategy : ITransactionProcessingStrategy
    {
        private readonly ITransactionProcessor _transactionProcessor;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;
        private readonly ProcessingOptions _options;
        
        public TransactionProcessingStrategy(
            ITransactionProcessor transactionProcessor, 
            IStateProvider stateProvider,
            IStorageProvider storageProvider, 
            ProcessingOptions options)
        {
            _transactionProcessor = transactionProcessor;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;
            _options = options;
        }
        
        private event EventHandler<TxProcessedEventArgs>? TransactionProcessedEvent; 
        
        event EventHandler<TxProcessedEventArgs> ITransactionProcessingStrategy.TransactionProcessed
        {
            add => TransactionProcessedEvent += value;
            remove => TransactionProcessedEvent -= value;
        } 
        
        public TxReceipt[] ProcessTransactions(Block block, ProcessingOptions processingOptions, IBlockTracer blockTracer, BlockReceiptsTracer receiptsTracer, IReleaseSpec spec)
        {
            for (int i = 0; i < block.Transactions.Length; i++)
            {
                Transaction currentTx = block.Transactions[i];
                ProcessTransaction(block, currentTx, i, receiptsTracer);
            }
            return receiptsTracer.TxReceipts!;
        }
        
        private void ProcessTransaction(Block block, Transaction currentTx, int index, BlockReceiptsTracer receiptsTracer)
        {
            if ((_options & ProcessingOptions.DoNotVerifyNonce) != 0)
            {
                currentTx.Nonce = _stateProvider.GetNonce(currentTx.SenderAddress);
            }

            receiptsTracer.StartNewTxTrace(currentTx);
            _transactionProcessor.Execute(currentTx, block.Header, receiptsTracer);
            receiptsTracer.EndTxTrace();

            TransactionProcessedEvent?.Invoke(this, new TxProcessedEventArgs(index, currentTx, receiptsTracer.TxReceipts[index]));
        }
    }
}

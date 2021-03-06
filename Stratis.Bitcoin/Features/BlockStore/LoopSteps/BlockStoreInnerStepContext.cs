﻿using NBitcoin;
using Stratis.Bitcoin.BlockStore;
using Stratis.Bitcoin.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stratis.Bitcoin.Features.BlockStore.LoopSteps
{
    /// <summary>
    /// Context for the inner steps, <see cref="BlockStoreInnerStepFindBlocks"/> and <see cref="BlockStoreInnerStepDownloadBlocks"/>.
    /// <para>
    /// The context also initializes the inner step <seealso cref="Routine"/>.
    /// </para>
    /// </summary>
    public sealed class BlockStoreInnerStepContext
    {
        public BlockStoreInnerStepContext(CancellationToken cancellationToken, BlockStoreLoop blockStoreLoop)
        {
            Guard.NotNull(blockStoreLoop, "blockStoreLoop");

            this.BlockStoreLoop = blockStoreLoop;
            this.CancellationToken = cancellationToken;
        }

        public BlockStoreInnerStepContext Initialize(ChainedBlock nextChainedBlock)
        {
            Guard.NotNull(nextChainedBlock, "nextChainedBlock");

            this.DownloadStack = new Queue<ChainedBlock>(new[] { nextChainedBlock });
            this.NextChainedBlock = nextChainedBlock;
            this.Routine = new List<BlockStoreInnerStep>() { new BlockStoreInnerStepFindBlocks(), new BlockStoreInnerStepDownloadBlocks() };

            this.InsertBlockSize = 0;
            this.StallCount = 0;
            this.Store = new List<BlockPair>();

            return this;
        }

        /// <summary>A queue of blocks to be downloaded.</summary>
        public Queue<ChainedBlock> DownloadStack { get; private set; }

        public BlockStoreLoop BlockStoreLoop { get; private set; }

        /// <summary>The chained block the inner step starts on.</summary>
        public ChainedBlock InputChainedBlock { get; private set; }

        public ChainedBlock NextChainedBlock { get; private set; }

        /// <summary>The routine (list of inner steps) the DownloadBlockStep executes.</summary>
        public List<BlockStoreInnerStep> Routine { get; private set; }

        public CancellationToken CancellationToken;

        /// <summary>
        /// A store of blocks that will be pushed to the repository once
        /// the <seealso cref="BlockStoreLoop.InsertBlockSizeThreshold"/> has been reached.
        /// </summary>
        public List<BlockPair> Store;

        public int InsertBlockSize;
        public int StallCount;

        /// <summary> Sets the next chained block to process.</summary>
        internal void GetNextBlock()
        {
            this.InputChainedBlock = this.NextChainedBlock;
            this.NextChainedBlock = this.BlockStoreLoop.Chain.GetBlock(this.InputChainedBlock.Height + 1);
        }

        /// <summary> Removes the BlockStoreInnerStepFindBlocks from the routine.</summary>
        internal void StopFindingBlocks()
        {
            this.Routine.Remove(this.Routine.OfType<BlockStoreInnerStepFindBlocks>().First());
        }
    }

    /// <summary>Abstract class that all DownloadBlockSteps implement</summary>
    public abstract class BlockStoreInnerStep
    {
        public abstract Task<BlockStoreLoopStepResult> ExecuteAsync(BlockStoreInnerStepContext context);
    }
}

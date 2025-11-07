using System.Reflection;
using System.Runtime.CompilerServices;

// UMAP Implementation Attribution
// This assembly includes a C# implementation of UMAP (Uniform Manifold Approximation and Projection)
// based on the original Python implementation by Leland McInnes.
//
// Original UMAP Implementation:
// Copyright (c) 2017, Leland McInnes
// Repository: https://github.com/lmcinnes/umap
// License: BSD 3-Clause (see UMAP/UMAP_LICENSE.txt)
//
// Citation:
// McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation and Projection
// for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
// https://arxiv.org/abs/1802.03426
//
// Additional Reference:
// Healy, J., McInnes, L. "Uniform manifold approximation and projection"
// Nat Rev Methods Primers 4, 82 (2024)
// https://doi.org/10.1038/s43586-024-00363-x

[assembly: InternalsVisibleTo("Flowthru.Extensions.ML.Tests")]

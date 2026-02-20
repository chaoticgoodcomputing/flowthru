# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
## [0.1.7](https://github.com/chaoticgoodcomputing/flowthru/compare/v0.1.6...v0.1.7) (2026-02-20)


### Bug Fixes

* nx-dotnet/dotnet setup order fix on action ([a3bc222](https://github.com/chaoticgoodcomputing/flowthru/commit/a3bc222e8b62bd991aae463a37c83ca96266f5b5))
* purge retaildata from test, sln records ([db63b23](https://github.com/chaoticgoodcomputing/flowthru/commit/db63b23c2538e9823b87f282a82b009baa5f9969))
* purge retaildata test ([9ba0376](https://github.com/chaoticgoodcomputing/flowthru/commit/9ba037690978ee38c4696ac52291de09b3b44500))
* remove languageext v5 upstream dep ([21b7554](https://github.com/chaoticgoodcomputing/flowthru/commit/21b7554ed36099760c09f066f1a24976d583c3f0))
* remove large data files that cause clone death ([9c8018f](https://github.com/chaoticgoodcomputing/flowthru/commit/9c8018f90a6807ed89a4e3cc78bcf2ec012894bc))
* remove unit test for discovery count ([b451f6d](https://github.com/chaoticgoodcomputing/flowthru/commit/b451f6d233e3b4cce53f53f3b4fc73ed04d609a5))

## [0.1.6](https://github.com/chaoticgoodcomputing/flowthru/compare/v0.1.5...v0.1.6) (2026-01-09)


### Bug Fixes

* multi-package static factory pattern for catalogentries ([b79370e](https://github.com/chaoticgoodcomputing/flowthru/commit/b79370e8d3c6beaaf92612ddbc59ad4504de20e6))

## [0.1.5](https://github.com/chaoticgoodcomputing/flowthru/compare/v0.1.4...v0.1.5) (2026-01-09)


### Bug Fixes

* consistent naming for catalog entry extensions in additional nuget packages ([ae2d97b](https://github.com/chaoticgoodcomputing/flowthru/commit/ae2d97b2e988c1ccd068f194944420a62806fea6))

## [0.1.4](https://github.com/chaoticgoodcomputing/flowthru/compare/v0.1.3...v0.1.4) (2025-12-28)


### Bug Fixes

* sync flowthru package versions across all packages ([f739025](https://github.com/chaoticgoodcomputing/flowthru/commit/f739025b398605a93a038b4955faaa6a12330cd6))

## [0.1.3](https://github.com/chaoticgoodcomputing/flowthru/compare/v0.1.2...v0.1.3) (2025-12-28)


### Features

* upload ML and ML.NET flowthru extensions as separate packages ([9898bc5](https://github.com/chaoticgoodcomputing/flowthru/commit/9898bc5a953da966f6aadd1e2ac9d97d125c5f94))

## [0.1.2](https://github.com/chaoticgoodcomputing/flowthru/compare/v0.1.1...v0.1.2) (2025-12-26)


### Bug Fixes

* split up CI/CD task into test and release ([380e594](https://github.com/chaoticgoodcomputing/flowthru/commit/380e59438abd7291b3fe6a5078fc203bd3635196))

## 0.1.1 (2025-12-26)


### Features

* ability to dry-run for preflight analysis ([fe61f51](https://github.com/chaoticgoodcomputing/flowthru/commit/fe61f516856774f9b827bca3023b4d81127360da))
* add dataset-datatype constraints to disallow nested schemas on data storage types that do not support nesting/non-primitive types ([a0609be](https://github.com/chaoticgoodcomputing/flowthru/commit/a0609be33daf48e96dcbcdd5e2895e627de4a9d8))
* add new MTG atlas example project ([8bc315c](https://github.com/chaoticgoodcomputing/flowthru/commit/8bc315c510359d6b13dde25ad3402f68a77182bb))
* additional comparison pipeline ([2bae1f4](https://github.com/chaoticgoodcomputing/flowthru/commit/2bae1f4bbd48c5c7d9a560f6a8a4348f8eb4d738))
* allow for nodata (unit) node input and ouput ([3c7874e](https://github.com/chaoticgoodcomputing/flowthru/commit/3c7874ee066bdd6e4bc4def84faf8bbeadfc06ff))
* allow plaintext description on pipeline nodes ([83fd041](https://github.com/chaoticgoodcomputing/flowthru/commit/83fd041ca552237f09d0b66afab8af456424b720))
* allow schemas to have required members ([31dc519](https://github.com/chaoticgoodcomputing/flowthru/commit/31dc5192da56f700def0c502d242f29768c1775e))
* begin testing framework ([074f37e](https://github.com/chaoticgoodcomputing/flowthru/commit/074f37ed07b63948fadfa6219f00e7f269ae2d47))
* catalog-level definition of inspection levels ([c6ef393](https://github.com/chaoticgoodcomputing/flowthru/commit/c6ef393e23d2916e28b353246c9b0e1fbe945b8c))
* configuration options for metadata providers ([4eb2596](https://github.com/chaoticgoodcomputing/flowthru/commit/4eb2596ddab58bb7ae45ef1a55cd9fbc808ac265))
* consolidate setup into FlowthruApplication builder, migrate critical code from test-flights to library code ([1fbc156](https://github.com/chaoticgoodcomputing/flowthru/commit/1fbc156ea880362fecedf7cafb578679b0e34409))
* create centralized SerializedLabel anno for universal label serialization ([a6ca4e1](https://github.com/chaoticgoodcomputing/flowthru/commit/a6ca4e11c15f4f0522f540f27c88c33fd8671ed3))
* create oracle text full/partial embedding dataset ([8ea8e29](https://github.com/chaoticgoodcomputing/flowthru/commit/8ea8e2946c70216da02bf0b7fdeabe3091631b11))
* custom ratchet testing architecture ([b9ce539](https://github.com/chaoticgoodcomputing/flowthru/commit/b9ce5390bbfa4184badb29ef5014f6b2cc5b7ea8))
* embedding pipeline example ([e0d5276](https://github.com/chaoticgoodcomputing/flowthru/commit/e0d5276e538f0607d572d1066ac45e24be330981))
* enum serialization annotations ([a3eb127](https://github.com/chaoticgoodcomputing/flowthru/commit/a3eb1272c793bab51039f60c22c861b76d9491a5))
* flexible & extensible catalogentries ([e676bc1](https://github.com/chaoticgoodcomputing/flowthru/commit/e676bc12c1720a29b101cc7106915fc4a55145f5))
* force increment change ([06d3522](https://github.com/chaoticgoodcomputing/flowthru/commit/06d3522b3c2d90ab371cb80aeb5c4c7cb98559e8))
* functional ml.net wrapper start ([958b29a](https://github.com/chaoticgoodcomputing/flowthru/commit/958b29a82a3c10730731a8f55feb47023c802af8))
* further mast coverage ([b276d6c](https://github.com/chaoticgoodcomputing/flowthru/commit/b276d6c4e77d6c95674812e32ae48ad3a731cf03))
* generic file catalog entry ([e9794aa](https://github.com/chaoticgoodcomputing/flowthru/commit/e9794aac2f0fb0bf7f48fe7f47aaa78686f64ea5))
* initial library setup ([e1312a3](https://github.com/chaoticgoodcomputing/flowthru/commit/e1312a3332453dbeea26057ba1e5881697a05f90))
* internal umap timing reports ([7f79ba4](https://github.com/chaoticgoodcomputing/flowthru/commit/7f79ba49f3a2fac2507e8c1d306ae2ae9c52bc2c))
* json data catalog implementation ([0918226](https://github.com/chaoticgoodcomputing/flowthru/commit/0918226b2fc435fe06210fb69997df71c2aba5db))
* magic AST ([94b4d99](https://github.com/chaoticgoodcomputing/flowthru/commit/94b4d99a2f2cb0997b7966b05ce9c01c44185645))
* magic atlas accurate umap ([488b81c](https://github.com/chaoticgoodcomputing/flowthru/commit/488b81c6ae11d46e18be226de0ead44d1ffe78ef))
* magic atlas embedding distribution analysis ([3871d2d](https://github.com/chaoticgoodcomputing/flowthru/commit/3871d2dc1f9e54f316a3b41b868c2533f394c2ce))
* magicatlas k-means clustering analytics ([e1f756d](https://github.com/chaoticgoodcomputing/flowthru/commit/e1f756df2441f9d448506d7c88167bc77f445ba7))
* magicatlas k-means clustering analytics ([cebd25c](https://github.com/chaoticgoodcomputing/flowthru/commit/cebd25c9133a6834a9cb6668f61be3b6a02b0f94))
* mast error code system, superpower install ([4578f93](https://github.com/chaoticgoodcomputing/flowthru/commit/4578f931e312cea92a2e88f930d6c4640d22be34))
* mast revamp, unit tests ([7fb7611](https://github.com/chaoticgoodcomputing/flowthru/commit/7fb7611b19ce84153294c7c094ac7d36911a0d1d))
* mast testing, ast ([55c632c](https://github.com/chaoticgoodcomputing/flowthru/commit/55c632c605535488bf63761f362ee62a2309b26c))
* metadata analysis in JSON and Mermaid form ([ce56eae](https://github.com/chaoticgoodcomputing/flowthru/commit/ce56eae2f27b302e9ff5a28a9f3286ee3a71a69a))
* move x-validate to data science portion to spice dag ([c9523aa](https://github.com/chaoticgoodcomputing/flowthru/commit/c9523aaa59444be894f4a88a753e888425951340))
* nuget publish action flow ([71c9e49](https://github.com/chaoticgoodcomputing/flowthru/commit/71c9e49c328c700c27864fecf5648b3fd1d93998))
* pca clustering, pipeline ([b806414](https://github.com/chaoticgoodcomputing/flowthru/commit/b80641489743693dd7ced049b693be812e48a7b5))
* pipeline builder addnode overloads ([a619aeb](https://github.com/chaoticgoodcomputing/flowthru/commit/a619aeb3b8778f0202e4ef6dbebb6faa0db9acbf))
* pure kedro spaceflights example ([cc5b8ac](https://github.com/chaoticgoodcomputing/flowthru/commit/cc5b8ac9d72b4d963ce637dd65b3548a035c9200))
* required parameter support for schemas ([ac9aec1](https://github.com/chaoticgoodcomputing/flowthru/commit/ac9aec1f10a9aa6ed4a2b02ab9bc431955d78bde))
* retail data example ([ecfdcda](https://github.com/chaoticgoodcomputing/flowthru/commit/ecfdcdafc60c5ef551a2aba2b62456b1923d9c52))
* run all pipelines as unified dag ([68da6ab](https://github.com/chaoticgoodcomputing/flowthru/commit/68da6abfae0e48c8c3da8d9615476d03165ee65b))
* scryfall card processing ([79aa48b](https://github.com/chaoticgoodcomputing/flowthru/commit/79aa48be50fabe2166e4243b056aa871c6d2ce70))
* simplify node pipeline registration type param requirements to remove redundant information ([69efa58](https://github.com/chaoticgoodcomputing/flowthru/commit/69efa58330024f746e5dcc752f5b07a3b9ac2f79))
* sparse matrix optimizations ([ebd6988](https://github.com/chaoticgoodcomputing/flowthru/commit/ebd6988382ee4a8898fae98cb197f4ed9fcb12c7))
* starter umap comparison tests ([8dbc537](https://github.com/chaoticgoodcomputing/flowthru/commit/8dbc5372db126f171f0ae773fc83e7855e81ccdd))
* strategized umap ([c69eb27](https://github.com/chaoticgoodcomputing/flowthru/commit/c69eb271480deb1859001266cfc473e1eb79825a))
* tiebreaking jiggle on atlas umap, to break up single-value columns ([5bb4522](https://github.com/chaoticgoodcomputing/flowthru/commit/5bb4522de1a5d3e0391b8b9f830dc5b79c152c9f))
* umap first stab ([50f0552](https://github.com/chaoticgoodcomputing/flowthru/commit/50f05529c4dbbb2b0a7a232a5afeb7c57b8c80f0))
* umap graph init and gradiant speedups ([71dfb07](https://github.com/chaoticgoodcomputing/flowthru/commit/71dfb0709015546dca542a5d81f43010ae39fcfa))
* umap optimizations ([d51477d](https://github.com/chaoticgoodcomputing/flowthru/commit/d51477d71bd97f7714aae6eb7226bb12bb282347))
* umap optimizations, parallelization ([eaa2418](https://github.com/chaoticgoodcomputing/flowthru/commit/eaa2418dee8eddec1d15af56f9a4f1b8daa79ebf))
* umap opts, applied to magic atlas ([d910e11](https://github.com/chaoticgoodcomputing/flowthru/commit/d910e1125c186caf650ad6365a59fca56dfe393c))
* use verify for ratchet-style testing ([0ccea9f](https://github.com/chaoticgoodcomputing/flowthru/commit/0ccea9f74ba44523ab47ff3dda475b5f4f84bac1))
* vscode snippets ([1aca658](https://github.com/chaoticgoodcomputing/flowthru/commit/1aca65878abd705cd3badb77e788220706efe896))


### Bug Fixes

* add datasets docs structure and initial kedro data ([f787cc6](https://github.com/chaoticgoodcomputing/flowthru/commit/f787cc6560e4f9da9cd25a445773191c0b6713ce))
* add missing assertion in test method ([ed5f3f1](https://github.com/chaoticgoodcomputing/flowthru/commit/ed5f3f1c0dbbb37b03af34d59dccf37e0dee9619))
* add missing curly in tests ([f161ce5](https://github.com/chaoticgoodcomputing/flowthru/commit/f161ce5dafc57938dddb0423e2beabdabe3dea9b))
* add retail data example to solution ([7aa36f2](https://github.com/chaoticgoodcomputing/flowthru/commit/7aa36f217ce2067f75dc17cab690bcce4b839762))
* add self-reference graph construction tests ([3a53d79](https://github.com/chaoticgoodcomputing/flowthru/commit/3a53d79ae16a93fdacd420002cf9aac1150efe57))
* allow CI workflow to run when called by release workflow ([5d5cf69](https://github.com/chaoticgoodcomputing/flowthru/commit/5d5cf697ae7d4a6e5f7a68a8a234e524ef7d4c47))
* better mast reporting ([9791efd](https://github.com/chaoticgoodcomputing/flowthru/commit/9791efd0b13e6197e1ec3addb3a88b39cba7f81a))
* configure Chromium for CI environment ([43907d5](https://github.com/chaoticgoodcomputing/flowthru/commit/43907d5c14813f3b1847f458d0b85d52bdd92234))
* consolidate CI and release into single workflow ([42ea370](https://github.com/chaoticgoodcomputing/flowthru/commit/42ea37079ca067d470a2e258ab8e9c5f3ef64df4))
* correct baseline UMAP implementation ([061d31b](https://github.com/chaoticgoodcomputing/flowthru/commit/061d31b4df182a28cb3926672c4137a20c89e014))
* fix compilation in atlas umap ([82d85d4](https://github.com/chaoticgoodcomputing/flowthru/commit/82d85d430ced5f53939e60663c7824eab8453754))
* fix data io directory paths, catalog ([cc4f246](https://github.com/chaoticgoodcomputing/flowthru/commit/cc4f246cb6be495ce5e16dd532ba7f472abe4f86))
* fix json (de)serial for nested dict ([403a9d1](https://github.com/chaoticgoodcomputing/flowthru/commit/403a9d1ea78af9afa964c8ad377cf5a1cdbd6f03))
* fix parquet deserialization of enum types ([6df3267](https://github.com/chaoticgoodcomputing/flowthru/commit/6df3267eec7d1fbeec4b8cdd7656711fdf868066))
* include Kedro reference data for KedroSpaceflights.Custom ([67474fa](https://github.com/chaoticgoodcomputing/flowthru/commit/67474fa7b493352d763b2de6e60222ef4af513c2))
* include kedro source data, include notice for license ([9a65834](https://github.com/chaoticgoodcomputing/flowthru/commit/9a658343414b23afe1fc43904af00ae36abaa25a))
* mast cost processing ([0324b23](https://github.com/chaoticgoodcomputing/flowthru/commit/0324b230f928f7e9078491f6b19f5b60809c28df))
* match ml.next namespaces with ml.net ([75d782c](https://github.com/chaoticgoodcomputing/flowthru/commit/75d782c2a3f67ac82d04a0c4875e6fe15da93b8e))
* minor updates ([32445d5](https://github.com/chaoticgoodcomputing/flowthru/commit/32445d5f3c47a5cfda3c1ef72ec3d553fa05d64c))
* ml.next test singlethreading to avoid RNG race condition ([8a85030](https://github.com/chaoticgoodcomputing/flowthru/commit/8a85030f58fb46cd5e5c606286e6e7d9bb40a5ad))
* move-away from phantom types within UMAP metrics ([c5441a1](https://github.com/chaoticgoodcomputing/flowthru/commit/c5441a17136793ba647901468cfa526890a811ec))
* parquet load issues ([7119849](https://github.com/chaoticgoodcomputing/flowthru/commit/7119849bd4236c3bc6fcb93ec97277716aaec326))
* parquet loading tactics updated to correct nulls ([4130509](https://github.com/chaoticgoodcomputing/flowthru/commit/4130509ba1d50ebfce674c69755f23339520619a))
* remove columnname ([a490de0](https://github.com/chaoticgoodcomputing/flowthru/commit/a490de0b4f65aa1ffd2964ff9784d67bf2b4ec35))
* remove deprecated alpha libraries ([df73c62](https://github.com/chaoticgoodcomputing/flowthru/commit/df73c62523039a5f8aa3e9c0ddd0fd1b9b445b53))
* remove side effects from image render nodes ([5fce6f5](https://github.com/chaoticgoodcomputing/flowthru/commit/5fce6f596dd4934833be91c0327a399b2f2f9ed3))
* resolve API surface areas between MagicAtlas->MagicAST ([2faf982](https://github.com/chaoticgoodcomputing/flowthru/commit/2faf98264d2dca31e48db7a45c4dda0ac248f920))
* resolve issues with NX command. ([3f98a9e](https://github.com/chaoticgoodcomputing/flowthru/commit/3f98a9ead90d8fc89e5c5eff20128146cc4f957a))
* rollback umap opts ([6f5e329](https://github.com/chaoticgoodcomputing/flowthru/commit/6f5e32965f1aa47df71ed5b57182cffa688b4522))
* separate reviews preprocessing to separate node, strengthen null value check with PDV ([2976e6e](https://github.com/chaoticgoodcomputing/flowthru/commit/2976e6e11201a941d9be156138d28becb24af06e))


### Documentation

* add dotnext ([e20c9f9](https://github.com/chaoticgoodcomputing/flowthru/commit/e20c9f91f1d24bf342527d69dfc688d69818d660))
* doc comments on pure spaceflights ([9fcfb5c](https://github.com/chaoticgoodcomputing/flowthru/commit/9fcfb5c44b1b5b20075f61f5aa0ad0fc8eb4ed83))
* minor atlas doc changes ([b56039e](https://github.com/chaoticgoodcomputing/flowthru/commit/b56039ee7b9128bf12661406995256bc2ff1b893))
* test suite docs ([4ae8eca](https://github.com/chaoticgoodcomputing/flowthru/commit/4ae8ecaef65f6e600c0829e84a3b384b9ac3a3b8))
* tutorial improvements ([3488267](https://github.com/chaoticgoodcomputing/flowthru/commit/3488267c20e750c428442fa6d7c22a64ea1675c7))
* update flowthru docs ([fbe08f5](https://github.com/chaoticgoodcomputing/flowthru/commit/fbe08f5b9196aca0e0dbcbaad9fa05f02c184a92))
* update readme ([929cc64](https://github.com/chaoticgoodcomputing/flowthru/commit/929cc6413ab4b4db0aea5f1283a120b66d860845))
* xdocs cleanup ([5ad7d2e](https://github.com/chaoticgoodcomputing/flowthru/commit/5ad7d2ef778ae6d4458df88e23bc23e303c8a4c8))
* xdocs cython ([bc58f7a](https://github.com/chaoticgoodcomputing/flowthru/commit/bc58f7ae50db6c0570c64920f3c5de3a4495a193))

## [0.1.0] - 2025-12-25

### Added

- Initial changelog start
- NuGet publish

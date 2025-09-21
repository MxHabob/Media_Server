const fg = require('fast-glob');
const path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const CopyPlugin = require('copy-webpack-plugin');
const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const { DefinePlugin, IgnorePlugin } = require('webpack');
const packageJson = require('./package.json');

const Assets = [
  'native-promise-only/npo.js',
  'libarchive.js/dist/worker-bundle.js',
  'libarchive.js/dist/libarchive.wasm',
  '@jellyfin/libass-wasm/dist/js/default.woff2',
  '@jellyfin/libass-wasm/dist/js/subtitles-octopus-worker.js',
  '@jellyfin/libass-wasm/dist/js/subtitles-octopus-worker.wasm',
  '@jellyfin/libass-wasm/dist/js/subtitles-octopus-worker-legacy.js',
  'pdfjs-dist/build/pdf.worker.js',
  'libpgs/dist/libpgs.worker.js',
];

const DEV_MODE = process.env.NODE_ENV !== 'production';
let COMMIT_SHA = 'unknown';
try {
  COMMIT_SHA = require('child_process')
    .execSync('git describe --always --dirty')
    .toString()
    .trim();
} catch (err) {
  console.warn('Failed to get commit SHA. Is git installed?', err);
}

const NODE_MODULES_REGEX = /[\\/]node_modules[\\/]/;

const THEMES = fg.globSync('themes/**/*.scss', { cwd: path.resolve(__dirname, 'src') });
const THEMES_BY_ID = THEMES.reduce((acc, theme) => {
  const themeId = theme.substring(0, theme.lastIndexOf('/')).replace('themes/', '');
  acc[`themes/${themeId}`] = `./${theme}`;
  return acc;
}, {});

module.exports = {
  context: path.resolve(__dirname, 'src'),
  target: 'browserslist',
  mode: DEV_MODE ? 'development' : 'production',
  entry: {
    'main.jellyfin': './index.jsx',
    ...THEMES_BY_ID,
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js', '.jsx'],
    modules: [path.resolve(__dirname, 'src'), 'node_modules'],
    alias: {
      cldr: require.resolve('cldrjs'),
      'cldr/event': require.resolve('cldrjs/dist/cldr/event'),
      'cldr/supplemental': require.resolve('cldrjs/dist/cldr/supplemental'),
    },
  },
  plugins: [
    new DefinePlugin({
      __COMMIT_SHA__: JSON.stringify(COMMIT_SHA),
      __JF_BUILD_VERSION__: JSON.stringify(
        process.env.WEBPACK_SERVE ? 'Dev Server' : process.env.JELLYFIN_VERSION || 'Release'
      ),
      __PACKAGE_JSON_NAME__: JSON.stringify(packageJson.name),
      __PACKAGE_JSON_VERSION__: JSON.stringify(packageJson.version),
      __USE_SYSTEM_FONTS__: JSON.stringify(process.env.USE_SYSTEM_FONTS || '0'),
      __WEBPACK_SERVE__: JSON.stringify(process.env.WEBPACK_SERVE || '0'),
    }),
    new CleanWebpackPlugin(),
    new HtmlWebpackPlugin({
      filename: 'index.html',
      template: 'index.html',
      hash: true,
      chunks: ['main.jellyfin', 'serviceworker'],
    }),
    new CopyPlugin({
      patterns: [
        { from: 'assets', to: 'assets' },
        { from: 'config.json', to: '.' },
        { from: 'robots.txt', to: '.' },
        {
          from: 'touchicon*.png',
          context: path.resolve(__dirname, 'node_modules/@jellyfin/ux-web/favicons'),
          to: 'favicons',
        },
        ...Assets.map((asset) => ({
          from: path.resolve(__dirname, `node_modules/${asset}`),
          to: 'libraries',
        })),
      ],
    }),
    new ForkTsCheckerWebpackPlugin({
      typescript: {
        configFile: path.resolve(__dirname, 'tsconfig.json'),
      },
    }),
    new MiniCssExtractPlugin({
      filename: ({ chunk }) =>
        chunk.name.startsWith('themes/') ? '[name]/theme.css' : '[name].[contenthash].css',
      chunkFilename: '[name].[contenthash].css',
    }),
  ],
  output: {
    filename: ({ chunk }) => (chunk.name === 'serviceworker' ? '[name].js' : '[name].bundle.js'),
    chunkFilename: '[name].[contenthash].chunk.js',
    assetModuleFilename: ({ filename }) => {
      if (filename === 'manifest.json') return '[base]';
      if (filename.startsWith('assets/') || filename.startsWith('themes/'))
        return '[path][base][query]';
      return '[name].[hash][ext][query]';
    },
    path: path.resolve(__dirname, 'dist'),
    publicPath: '/',
    clean: true,
  },
  optimization: {
    runtimeChunk: 'single',
    splitChunks: {
      chunks: 'all',
      maxInitialRequests: Infinity,
      cacheGroups: {
        vendors: {
          test: NODE_MODULES_REGEX,
          name: (module) => {
            const match = module.context.match(/[\\/]node_modules[\\/](.*?)([\\/]|$)/);
            if (!match) return 'vendors';
            const packageName = match[1];
            if (packageName.startsWith('@')) {
              const parts = module.context
                .substring(module.context.lastIndexOf(packageName))
                .split(/[\\/]/);
              return `vendors.${parts[0]}.${parts[1]}`;
            }
            if (packageName === 'date-fns') {
              const parts = module.context
                .substring(module.context.lastIndexOf(packageName))
                .split(/[\\/]/);
              let name = `vendors.${parts[0]}`;
              if (parts[1]) {
                name += `.${parts[1]}`;
                if (parts[1] === 'locale' && parts[2]) name += `.${parts[2]}`;
              }
              return name;
            }
            return `vendors.${packageName}`;
          },
          priority: -10,
        },
      },
    },
  },
  cache: {
    type: 'filesystem',
    buildDependencies: {
      config: [__filename],
    },
  },
  module: {
    rules: [
      {
        test: /\.html$/,
        use: 'html-loader',
      },
      {
        test: /\.(js|jsx|mjs)$/,
        include: [
          path.resolve(__dirname, 'src'),
          /node_modules\/(@jellyfin|@mui|@react-hook|@tanstack|axios|blurhash|compare-versions|date-fns|dom7|epubjs|flv\.js|highlight-words|libarchive\.js|linkify-it|markdown-it|material-react-table|mdurl|proxy-polyfill|punycode|react-blurhash|react-lazy-load-image-component|react-router|remove-accents|screenfull|ssr-window|swiper|usehooks-ts)/,
        ],
        resolve: { fullySpecified: false },
        use: {
          loader: 'babel-loader',
          options: {
            cacheDirectory: true,
            cacheCompression: false,
          },
        },
      },
      {
        test: /\.worker\.ts$/,
        exclude: /node_modules/,
        use: [
          'worker-loader',
          {
            loader: 'ts-loader',
            options: { transpileOnly: true },
          },
        ],
      },
      {
        test: /\.(ts|tsx)$/,
        exclude: /node_modules/,
        use: {
          loader: 'ts-loader',
          options: { transpileOnly: true },
        },
      },
      {
        test: /\.js$/,
        include: [
          path.resolve(__dirname, 'node_modules/pdfjs-dist'),
          path.resolve(__dirname, 'node_modules/xmldom'),
        ],
        use: {
          loader: 'babel-loader',
          options: {
            cacheDirectory: true,
            cacheCompression: false,
            plugins: ['@babel/transform-modules-umd'],
          },
        },
      },
      {
        test: /\.(sa|sc|c)ss$/i,
        oneOf: [
          {
            include: [path.resolve(__dirname, 'src/themes/')],
            use: [
              { loader: MiniCssExtractPlugin.loader, options: { publicPath: '../../' } },
              'css-loader',
              {
                loader: 'postcss-loader',
                options: {
                  postcssOptions: { config: path.resolve(__dirname, 'postcss.config.js') },
                },
              },
              'sass-loader',
            ],
          },
          {
            use: [
              DEV_MODE ? 'style-loader' : MiniCssExtractPlugin.loader,
              'css-loader',
              {
                loader: 'postcss-loader',
                options: {
                  postcssOptions: { config: path.resolve(__dirname, 'postcss.config.js') },
                },
              },
              'sass-loader',
            ],
          },
        ],
      },
      {
        test: /\.(ico|png|jpg|gif|svg)$/i,
        type: 'asset/resource',
      },
      {
        test: /\.(woff|woff2|eot|ttf|otf)$/,
        type: 'asset/resource',
      },
      {
        test: /\.(mp3)$/i,
        type: 'asset/resource',
      },
      {
        test: require.resolve('jquery'),
        loader: 'expose-loader',
        options: {
          exposes: ['$', 'jQuery'],
        },
      },
    ],
  },
};
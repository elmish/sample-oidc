const path = require("path");
const webpack = require("webpack");
const CopyWebpackPlugin = require('copy-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MinifyPlugin = require("terser-webpack-plugin");

const isProduction = process.argv.indexOf("serve") < 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

const commonPlugins = [
    new HtmlWebpackPlugin({
        filename: './index.html',
        template: './src/index.html'
    }),
    new CopyWebpackPlugin({
        patterns: [
            { from: './src/silent-renew.html' }
        ]
    })
];

module.exports = {
    mode: "development",
    devtool: isProduction ? false : "source-map",
    entry: isProduction ?
    {
        demo: [
            './src/out/app.js'
        ]
    } : {
        app: [
            './src/out/app.js'
        ]
    },
    output: {
        path: path.join(__dirname, "./out"),
        filename: isProduction ? '[name].[contenthash].js' : '[name].js',
        publicPath: isProduction ? "./" : "/"
    },
    optimization : {
        splitChunks: {
            cacheGroups: {
                commons: {
                    test: /node_modules/,
                    name: "vendors",
                    chunks: "all"
                }
            }
        },
        minimizer: isProduction
            ? [new MinifyPlugin()]
            : []
    },
    devServer: {
        port: 8090,
        static: {
            directory: './out'
        },
        historyApiFallback: true
    },
    module: {
        rules: [
            {
                test: /\.js$/,
                enforce: "pre",
                use: ["source-map-loader"],
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: {
                        presets: [
                            ["@babel/preset-env", {
                                "modules": false,
                                "useBuiltIns": "usage",
                                "corejs": 3
                            }]
                        ],
                    }
                },
            }
        ]
    },
    plugins: isProduction
        ? commonPlugins
        : commonPlugins.concat([
            new webpack.HotModuleReplacementPlugin()
        ])
}

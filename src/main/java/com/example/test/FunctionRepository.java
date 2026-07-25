package com.example.test;

import android.app.Application;
import android.os.AsyncTask;
import android.os.Build;

import androidx.annotation.RequiresApi;

import java.util.Iterator;
import java.util.List;

public class FunctionRepository {
    private FunctionDao functionDao;

    public FunctionRepository(Application application) {
        FunctionDatabase database = FunctionDatabase.getInstance(application);
        functionDao = database.functionDao();
    }

    public void insert(Function function) {
        new InsertMonsterAsyncTask(functionDao).execute(function);
    }

    public void update(Function function) {
        new UpdateMonsterAsyncTask(functionDao).execute(function);
    }

    public void delete(Function function) {
        new DeleteMonsterAsyncTask(functionDao).execute(function);
    }

    public Function findFunctionByName(String name) {
        return functionDao.getFunctionByName(name);
    }

    public List<Function> getAllFunctions() {
        return functionDao.getAllFunctions();
    }

    private static class InsertMonsterAsyncTask extends AsyncTask<Function, Void, Void> {
        private FunctionDao functionDao;

        private InsertMonsterAsyncTask(FunctionDao functionDao){
            this.functionDao = functionDao;
        }
        @Override
        protected Void doInBackground(Function... functions) {
            functionDao.insert(functions[0]);
            return null;
        }
    }

    private static class UpdateMonsterAsyncTask extends AsyncTask<Function, Void, Void> {
        private FunctionDao functionDao;

        private UpdateMonsterAsyncTask(FunctionDao functionDao){
            this.functionDao = functionDao;
        }
        @Override
        protected Void doInBackground(Function... functions) {
            functionDao.update(functions[0]);
            return null;
        }
    }

    private static class DeleteMonsterAsyncTask extends AsyncTask<Function, Void, Void> {
        private FunctionDao functionDao;

        private DeleteMonsterAsyncTask(FunctionDao functionDao){
            this.functionDao = functionDao;
        }
        @Override
        protected Void doInBackground(Function... functions) {
            functionDao.delete(functions[0]);
            return null;
        }
    }
}
